FROM node:22-alpine AS build

WORKDIR /web
COPY src/AmiiboRewards.Web/package.json src/AmiiboRewards.Web/package-lock.json ./
RUN npm ci
COPY src/AmiiboRewards.Web/ ./
RUN npm run build

FROM nginx:1.27-alpine
COPY src/AmiiboRewards.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 80
