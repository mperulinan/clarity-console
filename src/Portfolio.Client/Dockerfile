# Stage 1: Build the Angular application
FROM node:22-alpine AS build
WORKDIR /app

# Copy package.json and package-lock.json first for caching
COPY package*.json ./
RUN npm ci

# Copy the rest of the application code
COPY . .

# Build the application
RUN npm run build -- --configuration production

# Stage 2: Serve the application with Nginx
FROM nginx:alpine
# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Copy built files from the previous stage
COPY --from=build /app/dist/Portfolio.Client/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
