var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BlueChat_Mobile>("bluechat-mobile");

builder.Build().Run();