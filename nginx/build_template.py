import os
import re
from sys import argv    
from dotenv import load_dotenv

load_dotenv()

class Certificate:
    def __init__(self, name, cert_path, key_path):
        self.name = name
        self.cert_path = cert_path
        self.key_path = key_path

    def __repr__(self):
        return f"Certificate(name={self.name}, cert_path={self.cert_path}, key_path={self.key_path})"

class Location:
    def __init__(self, name, url, order=0):
        self.name = name
        self.url = url
        self.order = 0

    def __repr__(self):
        return f"Location(name={self.name}, url={self.url})"    

class Subdomain:
    def __init__(self, name, certificate: Certificate = None):
        self.name = name
        self.locations: list[Location] = []
        self.certificate: Certificate = certificate
        self.default_server = False

    def add_location(self, location):
        self.locations.append(location)

    def has_cert(self):
        return self.certificate is not None

    def add_location(self, location):
        self.locations.append(location)

    def __repr__(self):
        return f"Subdomain(name={self.name}, locations={self.locations})"

https_rewrite = bool(os.environ.get("NGOPT_HTTPS_REWRITE", True))
default_cert_name = os.environ.get("NGOPT_DEFAULT_CERT", None)

certs: dict[str, Certificate] = {}
subdomains: dict[str, Subdomain] = {}

cert_pattern = re.compile(r"^NG_CERT_([^_]+)$")
for key, value in os.environ.items():
    match = cert_pattern.match(key)
    if match:
        cert_name = match.group(1)
        cert_path, key_path = value.split(";")
        if cert_path is None or key_path is None:
            raise ValueError(f"NG_CERT_{cert_name} or NG_KEY_{cert_name} is not set")
        
        certs[cert_name] = Certificate(cert_name, cert_path, key_path) 

default_cert = certs.get(default_cert_name, None)

domain_pattern = re.compile(r"^NG_DOMAIN_([^_]+)$")
for key, value in os.environ.items():
    match = domain_pattern.match(key)
    if match:
        domain_name = match.group(1)
        domain = Subdomain(domain_name)
        subdomains[domain_name] = domain
        cert_name = os.environ.get(f"NG_DOMAIN_{domain_name}_CERT", default_cert_name)
        domain.default_server = bool(os.environ.get(f"NG_DOMAIN_{domain_name}_DEFAULT_SERVER", False))
        if cert_name is not None:
            if cert_name not in certs:
                raise ValueError(f"NG_DOMAIN_{domain_name}_CERT is not set")
            domain.certificate = certs[cert_name]    


def get_or_create_domain(domain_name):
    if domain_name not in subdomains:
        subdomains[domain_name] = Subdomain(domain_name, default_cert)

    return subdomains[domain_name]

pattern = "^NG_LOCATION_([^_]+)$"
for key, value in os.environ.items():
    match = re.match(pattern, key)
    if match:
        location = match.group(1)
        domain = os.environ.get(f"NG_LOCATION_{location}_DOMAIN", None)
        url = os.environ.get(f"NG_LOCATION_{location}_URL", None)
        if url is None:
            raise ValueError(f"NG_LOCATION_{location}_URL is not set")
        
        domain = get_or_create_domain(domain)
        order = int(os.environ.get(f"NG_LOCATION_{location}_ORDER", len(domain.locations)))
        domain.add_location(Location(value, url, order))



nginx_conf = ""
if https_rewrite:
    nginx_conf += f"""
    server {{
        listen 80;
        server_name {' '.join(subdomains.keys())};
        return 301 https://$host$request_uri;
    }}

    """

for subdomain in subdomains.values():
    cert_block = f"""
    ssl_certificate {subdomain.certificate.cert_path};
    ssl_certificate_key {subdomain.certificate.key_path};
    """ if subdomain.has_cert() else ''
    location_block = ''
    for location in sorted(subdomain.locations, key=lambda x: x.order):
        location_block += f"""
        location {location.name} {{ 
            proxy_pass {location.url}; 
        }}

        """
    nginx_conf += f"""
    server {{
        listen 443 ssl http2 {'default_server' if subdomain.default_server else ''};
        server_name {subdomain.name};
        {cert_block}
        {location_block}
    }}

    """

print(nginx_conf, flush=True)
out_file = argv[1] if len(argv) > 1 else "nginx.conf"
with open(out_file, "w") as f:
    f.write(nginx_conf)