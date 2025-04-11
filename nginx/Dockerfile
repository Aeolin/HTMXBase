# === Stage 1: Build config and certs ===
FROM python:3.13-alpine as builder

RUN apk add --no-cache openssl gettext

# Create directories
RUN mkdir -p /build/certs /build/conf

# Generate self-signed cert
RUN openssl req -x509 -nodes -days 365 \
    -newkey rsa:2048 \
    -keyout /build/certs/selfsigned.key \
    -out /build/certs/selfsigned.crt \
    -subj "/CN=localhost"

RUN pip install python-dotenv

# Copy and process nginx config
COPY nginx.env /build/.env
COPY nginx.conf /build/nginx.conf
COPY build_template.py /build/build_template.py 

RUN python3 -u /build/build_template.py /build/default.conf

# === Stage 2: Final nginx image ===
FROM nginx:alpine

COPY --from=builder /build/nginx.conf /etc/nginx/nginx.conf
COPY --from=builder /build/default.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /build/certs /etc/nginx/certs
