# Stage 1: Build Angular App
FROM node:20 as build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
# bauen die App
RUN npm run build -- --configuration development

# Stage 2: Serve with Nginx
FROM nginx:alpine
COPY --from=build /app/dist/untitled3/browser /usr/share/nginx/html

# Nginx Config für Angular Routing
RUN echo "server { listen 80; root /usr/share/nginx/html; index index.html; location / { try_files \$uri \$uri/ /index.html; } }" > /etc/nginx/conf.d/default.conf
EXPOSE 80
