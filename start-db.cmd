docker-compose up -d db
timeout /t 15
docker-compose exec db  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P HakunaM4t4t4@171 -Q "CREATE DATABASE [HorusDb]"
docker-compose exec db  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P HakunaM4t4t4@171 -i setup.sql