source .env

docker-compose build db
docker-compose up -d db 
sleep 15
docker-compose exec db  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "CREATE DATABASE [HorusDb]"
docker-compose exec db  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -i setup.sql