container-up:
	docker compose up -d
container-down:
	docker compose down
build:
	docker compose down
	docker compose up --build -d
	docker image prune -f 
logs:
	docker compose logs -f
nest-server:
	@if [ -z "$(service)" ]; then \
		echo "Usage: make nest-server service=notification-service"; \
	else \
		cd services/nest-service && npm run serve $(service); \
	fi
mysql:
	docker start mysql-container
main-service:
	cd services/main-service && dotnet watch
noti-service:
	cd services/nest-service && npm run start notification-service
chat-service:
	cd services/nest-service && npm run start chat-service

gen-protobuf-main-service:
	protoc -I ./protos --include_imports --include_source_info \
		--descriptor_set_out=./proto.pb $(shell find ./protos -name "*.proto")

gen-protobuf-notification-service-wd:
	cd services\nest-service\apps\notification-service && \
	protoc --plugin=protoc-gen-ts_proto=..\..\node_modules\.bin\protoc-gen-ts_proto.cmd --ts_proto_out=.\src\types --ts_proto_opt=nestJs=true --proto_path=.\src\proto .\src\proto\notification.proto && \
	protoc -I .\src\proto -I .\src\proto\google\api --include_imports --include_source_info --descriptor_set_out=..\..\..\..\notification.pb .\src\proto\notification.proto

gen-protobuf-chat-service-wd:
	cd services\nest-service\apps\chat-service && \
	protoc --plugin=protoc-gen-ts_proto=..\..\node_modules\.bin\protoc-gen-ts_proto.cmd --ts_proto_out=.\src\types --ts_proto_opt=nestJs=true --proto_path=.\src\protos .\src\protos\chat.proto && \
	protoc -I .\src\protos -I .\src\protos\google\api --include_imports --include_source_info --descriptor_set_out=..\..\..\..\chat.pb .\src\protos\chat.proto

gen-protobuf-main-service-wd:
	powershell -Command "$$protos = Get-ChildItem -Recurse -Filter *.proto -Path './protos' | ForEach-Object { $$_.FullName | Resolve-Path -Relative }; protoc -I './protos' --include_imports --include_source_info --descriptor_set_out=./proto.pb $$protos"

gen-protobuf: 
	make gen-protobuf-main-service

gen-protobuf-wd: 
	make gen-protobuf-main-service-wd && make gen-protobuf-notification-service-wd 

gen-testtoken:
	cd services/node-service && node genjwt.js
sync:
	docker compose down 
	make gen-protobuf 
	docker compose up -d
sync-wd:
	docker compose down 
	make gen-protobuf-wd
	make build
cqlsh:
	docker exec -it cassandra cqlsh
all:
	make sync
	make main-service
	make noti-service
# 	make chat-service
.PHONY: container-up container-down nest-server gen-protobuf sync cqlsh main-service	