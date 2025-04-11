FROM alpine:latest

RUN mkdir /app
WORKDIR /app

COPY ./ /app
RUN apk add --no-cache jq curl

RUN chmod +x requests.sh
ENTRYPOINT ["/app/requests.sh"]