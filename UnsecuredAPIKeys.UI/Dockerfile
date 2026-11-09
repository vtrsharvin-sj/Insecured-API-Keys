# The UI is a statically exported Next.js site served by nginx, which also
# reverse-proxies the API on the same origin under /apiproxy/ - so a single
# port-forwarded URL serves the whole site.

# Build stage
FROM node:22-alpine AS build
ARG NEXT_PUBLIC_API_URL=/apiproxy
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL} \
    NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1
WORKDIR /app
COPY UnsecuredAPIKeys.UI/package.json UnsecuredAPIKeys.UI/
RUN cd UnsecuredAPIKeys.UI && npm install --legacy-peer-deps --no-audit --no-fund
COPY UnsecuredAPIKeys.UI/ UnsecuredAPIKeys.UI/
RUN cd UnsecuredAPIKeys.UI && npm run build:simple

# Serve stage
FROM nginx:alpine AS final
COPY --from=build /app/UnsecuredAPIKeys.UI/out /usr/share/nginx/html
COPY deploy/nginx-ui.conf /etc/nginx/conf.d/default.conf
EXPOSE 3000
