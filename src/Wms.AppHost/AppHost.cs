var builder = DistributedApplication.CreateBuilder(args);

builder.AddKubernetesEnvironment("k8s")
    .WithProperties(k8s =>
    {
        k8s.HelmChartName = "wms-ai-platform";
    });

var postgresPassword = builder.AddParameter(
    "postgres-password",
    "wms-dev-postgres-password",
    secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithPassword(postgresPassword)
    .WithDataVolume("wms-postgres-data");
var wmsDb = postgres.AddDatabase("wmsdb");
var aiDb = postgres.AddDatabase("aidb");

var redis = builder.AddRedis("redis")
    .WithDataVolume("wms-redis-data");
var rabbit = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("wms-rabbitmq-data");

_ = builder.AddContainer("minio", "quay.io/minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(targetPort: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithVolume("wms-minio-data", "/data")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin");

var domain = builder.AddProject<Projects.Wms_DomainService>("wms-domain-service")
    .WithReference(wmsDb)
    .WithReference(redis)
    .WithReference(rabbit);

var auth = builder.AddProject<Projects.Auth_Service>("auth-service")
    .WithReference(domain);

var runtime = builder.AddProject<Projects.Agent_Runtime>("agent-runtime")
    .WithReference(aiDb)
    .WithReference(redis)
    .WithReference(rabbit)
    .WithReference(domain);

var bff = builder.AddProject<Projects.Ops_Bff>("ops-bff")
    .WithReference(domain)
    .WithReference(runtime)
    .WithReference(auth);

builder.AddProject<Projects.Gateway_Yarp>("gateway-yarp")
    .WithReference(auth)
    .WithReference(bff)
    .WithReference(domain)
    .WithReference(runtime);

builder.Build().Run();
