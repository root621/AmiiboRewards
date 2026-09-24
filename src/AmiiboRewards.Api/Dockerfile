FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /workspace
COPY . .

# Restore during the build so container startup does not need network access.
RUN dotnet restore AmiiboRewards.slnx

EXPOSE 8080
CMD ["dotnet", "run", "--project", "src/AmiiboRewards.Api/AmiiboRewards.Api.csproj", "--no-restore", "--urls", "http://+:8080"]
