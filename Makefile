include .env
export $(shell sed 's/=.*//' .env)

stop-all:
	docker-compose stop

reset-db:
	docker-compose stop db
	docker-compose rm -v db

stop-db:
	docker-compose stop db

init-db:
	docker-compose build db
	docker-compose up -d db 
	sleep 15
	docker-compose exec db  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $$SA_PASSWORD -Q "CREATE DATABASE [HorusDb]" -C
	docker-compose exec db  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $$SA_PASSWORD -i setup.sql -C

init-redis:
	docker-compose up -d redis

stop-redis:
	docker-compose stop redis

init-databases: init-db init-redis

init-monitoring:
	docker-compose up -d  grafana influxdb telegraf

stop-monitoring:
	docker-compose stop grafana influxdb telegraf

init-api: build-api init-databases
	docker-compose up -d api

stop-api: stop-db stop-redis
	docker-compose stop api

build-api: 
	docker-compose build api

init-load-test: init-api init-monitoring
	docker-compose run k6 run /scripts/stages.js
