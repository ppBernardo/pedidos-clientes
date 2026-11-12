# syntax=docker/dockerfile:1

# 1) Build da SPA. Imagem Debian (glibc), e não alpine, porque TypeScript 7,
#    oxlint e rolldown usam binários nativos por plataforma.
FROM node:24-slim AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# 2) nginx serve os arquivos estáticos e faz o proxy de /api para a API.
FROM nginx:1.30-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
