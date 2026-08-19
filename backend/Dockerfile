FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/QuizSystem.Domain/QuizSystem.Domain.csproj src/QuizSystem.Domain/
COPY src/QuizSystem.Application/QuizSystem.Application.csproj src/QuizSystem.Application/
COPY src/QuizSystem.Infrastructure/QuizSystem.Infrastructure.csproj src/QuizSystem.Infrastructure/
COPY src/QuizSystem.Api/QuizSystem.Api.csproj src/QuizSystem.Api/
RUN dotnet restore src/QuizSystem.Api/QuizSystem.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/QuizSystem.Api/QuizSystem.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN mkdir -p /data /app/wwwroot/uploads/exam-images
COPY --from=build /app/publish/ ./
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/data/quizsystem.db"

EXPOSE 8080
ENTRYPOINT ["docker-entrypoint.sh"]
