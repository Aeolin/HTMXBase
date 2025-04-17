FROM alpine:latest

RUN mkdir /app
WORKDIR /app

COPY ./ /app
RUN apk add --no-cache jq curl dos2unix

RUN dos2unix /app/requests.sh
RUN chmod +x /app/requests.sh
ENTRYPOINT ["/app/requests.sh"]