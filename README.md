# Enterprise Fleet Management Platform

An event-driven, microservices-based fleet management platform built with .NET 8, PostgreSQL, TimescaleDB, PostGIS, RabbitMQ, and MQTT.

## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Architecture Overview
- **`src/BuildingBlocks/`**: Shared libraries (Domain, Contracts, Messaging, Auth, Observability).
- **`src/Services/`**: Independent microservices implementing Clean Architecture.
- **`src/ApiGateway/`**: YARP reverse proxy and gateway routing.
- **`deploy/`**: Docker Compose and deployment manifests.

## Getting Started
1. Start local infrastructure:
   ```bash
   docker compose -f deploy/docker-compose.yml up -d