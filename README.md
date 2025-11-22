## Building All Services at Once

To build all services in a single command:

```bash
docker build -f src/ChatBro.Server/Dockerfile -t chatbro-server:latest .
docker build -f src/ChatBro.RestaurantsService/Dockerfile -t chatbro-restaurants:latest .
```

