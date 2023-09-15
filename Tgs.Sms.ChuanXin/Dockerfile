#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Sms.ChuanXin/Tgs.Sms.ChuanXin.csproj", "Tgs.Sms.ChuanXin/"]
COPY ["ChuanXin.Core/ChuanXin.Core.csproj", "ChuanXin.Core/"]
RUN dotnet restore "Tgs.Sms.ChuanXin/Tgs.Sms.ChuanXin.csproj"
COPY . .
WORKDIR "/src/Tgs.Sms.ChuanXin"
RUN dotnet build "Tgs.Sms.ChuanXin.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Sms.ChuanXin.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Sms.ChuanXin.dll"]