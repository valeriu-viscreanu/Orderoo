FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["OrderApi.csproj", "./"]
RUN dotnet restore "OrderApi.csproj"

COPY . .
RUN dotnet publish "OrderApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderApi.dll"]
