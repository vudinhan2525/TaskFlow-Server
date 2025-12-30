# ======================
# INFRA (Kafka, Zookeeper, Envoy, v.v.)
# ======================
infra-up:
	docker compose up -d zookeeper kafka kafka-ui envoy

infra-down:
	docker compose down zookeeper kafka kafka-ui envoy

infra-logs:
	docker compose logs -f zookeeper kafka

# ======================
# SERVICES (NestJS, .NET, ...)
# ======================
build-services:
	docker compose up --build -d

# ======================
# DEVELOPMENT WORKFLOW
# ======================
sync:
	@echo "🔄 Syncing protobuf and restarting services (infra unchanged)..."
	make gen-protobuf
	docker compose up --build -d

logs:
	docker compose logs -f

# ======================
# NESTJS SERVICES
# ======================
nest-server:
	@if [ -z "$(service)" ]; then \
		echo "Usage: make nest-server service=notification-service"; \
	else \
		cd services/nest-service && npm run serve $(service); \
	fi

noti-service:
	cd services/nest-service && npm run start notification-service

chat-service:
	cd services/nest-service && npm run start chat-service

# ======================
# .NET MAIN SERVICE
# ======================
main-service:
	cd services/main-service && dotnet watch

# ======================
# PROTOBUF
# ======================
gen-protobuf-main-service:
	protoc -I ./protos --include_imports --include_source_info \
		--descriptor_set_out=./proto.pb $(shell find ./protos -name "*.proto")

gen-protobuf: 
	make gen-protobuf-main-service

# ======================
# UTILS
# ==================
container-up:
	docker compose up -d

container-down:
	docker compose down

cqlsh:
	docker exec -it cassandra cqlsh

gen-testtoken:
	cd services/node-service && node genjwt.js

# ======================
# FULL DEV START (services + infra)
# ======================
all:
	make infra-up
	make sync
	@echo "✅ Ready! Now run:"
	@echo "   - make main-service"
	@echo "   - make noti-service"

.PHONY: infra-up infra-down infra-logs build-services sync logs nest-server noti-service chat-service main-service gen-protobuf gen-protobuf-main-service container-up container-down cqlsh gen-testtoken all