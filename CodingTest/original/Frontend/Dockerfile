FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out

FROM nginx:alpine AS runtime

COPY --from=build /out/wwwroot /usr/share/nginx/html

COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80