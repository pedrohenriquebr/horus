# Define variables
PROJECT_NAME = Horus.RootBootstrapper
DOTNET = dotnet
# Define build targets
build:
	$(DOTNET) build ./Api/RootBootstrapper/$(PROJECT_NAME).csproj --arch x64 --os linux

clean:
	$(DOTNET) clean ./Api/RootBootstrapper/$(PROJECT_NAME).csproj 

# Define run target
watch:
	$(DOTNET) watch run --project $(PROJECT_NAME)/$(PROJECT_NAME).csproj --launch-profile $(PROFILE)

run: 
	ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:3000 $(DOTNET)  ./Api/RootBootstrapper/bin/Debug/net7.0/linux-x64/$(PROJECT_NAME).dll
