using Azure;
using Azure.Data.Tables;
using AzTwWebsiteApi.Models.Blog;
using Microsoft.Extensions.Logging;
using Moq;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Tests.Blog;

public class UpdateBlogPostFunctionTests
{
    private readonly Mock<ILogger<BlogPostFunctions>> _loggerMock;
    private readonly Mock<ILogger<TableStorageService<BlogPost>>> _tableStorageLoggerMock;
    private readonly Mock<IMetricsService> _metricsMock;
    private readonly HandleCrudFunctions _handleCrudFunctions;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly BlogPostFunctions _blogPostFunctions;

    public UpdateBlogPostFunctionTests()
    {
        _loggerMock = new Mock<ILogger<BlogPostFunctions>>();
        _metricsMock = new Mock<IMetricsService>();
        _tableStorageLoggerMock = new Mock<ILogger<TableStorageService<BlogPost>>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        
        _loggerFactoryMock.Setup(x => x.CreateLogger<TableStorageService<BlogPost>>())
            .Returns(_tableStorageLoggerMock.Object);
            
        _handleCrudFunctions = new HandleCrudFunctions(
            Mock.Of<ILogger<HandleCrudFunctions>>(),
            _metricsMock.Object,
            _loggerFactoryMock.Object);
            
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        Environment.SetEnvironmentVariable("BlogPostsTableName", "mockblog");

        _blogPostFunctions = new BlogPostFunctions(
            _loggerMock.Object,
            _handleCrudFunctions,
            _metricsMock.Object);
    }

    [Fact]
    public async Task UpdateBlogPost_ShouldPreserveExistingFields()
    {
        // Arrange
        var blogId = Guid.NewGuid().ToString();
        var existingPost = new BlogPost
        {
            Id = blogId,
            PartitionKey = blogId,
            RowKey = blogId,
            Title = "Original Title",
            Content = "Original Content",
            Status = "Draft",
            AuthorId = "author1",
            Tags = "tag1,tag2"
        };

        // First create the blog post
        var createOptions = new CrudOperationOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            Data = existingPost
        };

        await _handleCrudFunctions.HandleCrudOperation<BlogPost>(
            Constants.Storage.Operations.Set,
            "mockblog",
            createOptions);

        // Update with partial data
        var updateData = new BlogPost
        {
            Id = blogId,
            PartitionKey = blogId,
            RowKey = blogId,
            Title = "Updated Title",  // Only update title
            Status = "Published"      // And status
        };

        var updateOptions = new CrudOperationOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            Data = updateData
        };

        // Act
        var result = await _handleCrudFunctions.HandleCrudOperation<BlogPost>(
            Constants.Storage.Operations.Update,
            "mockblog",
            updateOptions);

        // Assert
        var updatedPost = result.Items.FirstOrDefault();
        Assert.NotNull(updatedPost);
        Assert.Equal("Updated Title", updatedPost.Title);         // Should be updated
        Assert.Equal("Published", updatedPost.Status);           // Should be updated
        Assert.Equal("Original Content", updatedPost.Content);   // Should be preserved
        Assert.Equal("author1", updatedPost.AuthorId);          // Should be preserved
        Assert.Equal("tag1,tag2", updatedPost.Tags);            // Should be preserved

        // Cleanup
        var deleteOptions = new CrudOperationOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            Filter = $"PartitionKey eq '{blogId}' and RowKey eq '{blogId}'"
        };

        await _handleCrudFunctions.HandleCrudOperation<BlogPost>(
            Constants.Storage.Operations.Delete,
            "mockblog",
            deleteOptions);
    }
}
