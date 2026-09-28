# Stage 1: build the static site (HTML, JS and CSS) with Node.
FROM node:24-alpine AS build
WORKDIR /app

# Install dependencies first so Docker can cache them while the code changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Stage 2: serve the built files with nginx, which also forwards API calls.
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
