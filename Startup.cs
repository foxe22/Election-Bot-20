// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.BotBuilderSamples.Bots;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder.Azure;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

             var cosmosDbStorageOptions = new CosmosDbPartitionedStorageOptions()
            {
                CosmosDbEndpoint = "https://electioncosmos.documents.azure.com:443/",
                AuthKey = "0zqSGejOcM7dqU603OFedsrfXf3HG6DBrwO0YZm85h2IlZrdyDY7la7tgfX0axd9ccNN4myrphorQMlxOuuBSw==",
                DatabaseId = "BotStoage",
                ContainerId = "Group2"
            };
            var storage = new CosmosDbPartitionedStorage(cosmosDbStorageOptions);

            services.AddSingleton<IStorage>(new CosmosDbPartitionedStorage(cosmosDbStorageOptions));

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            //var storageAccount = "DefaultEndpointsProtocol=https;AccountName=mybotstorage;AccountKey=76Xvfo8hI016W6IreBYyNz610suMmPzhJ2VbIV1VUanL8I7VXs2qCTdYeW7w/1cjfK9+UnX6C0f4J4hEfm90sg==;EndpointSuffix=core.windows.net";
            //var storageContainer = "mybotstorage";

            //services.AddSingleton<IStorage>(new AzureBlobStorage(storageAccount, storageContainer));

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register LUIS recognizer
            services.AddSingleton<ConversationRecognizer>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();
            services.AddSingleton<ElectionDialog>();
            services.AddSingleton<IssuesDialog>();
            services.AddSingleton<ConstituencyDialog>();
            services.AddSingleton<EndConversationDialog>();
            services.AddSingleton<PartyDialog>();
            services.AddSingleton<UserProfileDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseMvc();
        }
    }
}
