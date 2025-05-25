// C# app: GetBlogPostCommentsFunction.cs
// Endpoint: api/blog/posts/{id}/comments

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Data.Tables;
using AzTwWebsiteApi.Utils;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;