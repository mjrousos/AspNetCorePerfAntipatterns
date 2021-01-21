#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["CombinedDemo/CombinedDemo.csproj", "CombinedDemo/"]
RUN dotnet restore "CombinedDemo/CombinedDemo.csproj"
COPY . .
WORKDIR "/src/CombinedDemo"
RUN dotnet build "CombinedDemo.csproj" -c Release -o /app/build

# ## Install dotnet-trace, dotnet-counters, and dotnet-dump
# RUN dotnet tool install --tool-path /tools dotnet-trace
# RUN dotnet tool install --tool-path /tools dotnet-counters
# RUN dotnet tool install --tool-path /tools dotnet-dump
# RUN dotnet tool install --tool-path /tools dotnet-gcdump

FROM build AS publish
RUN dotnet publish "CombinedDemo.csproj" -c Release -o /app/publish

FROM base AS final

# ## Copy diagnostics tools
# WORKDIR /tools
# COPY --from=publish /tools .

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CombinedDemo.dll"]