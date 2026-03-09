FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Identity.Server/Identity.Server.csproj", "src/Identity.Server/"]
RUN dotnet restore "src/Identity.Server/Identity.Server.csproj"

COPY . .
WORKDIR "/src/src/Identity.Server"
RUN dotnet publish "Identity.Server.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish .

USER appuser
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Identity.Server.dll"]
