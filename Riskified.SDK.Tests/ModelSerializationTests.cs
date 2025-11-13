using Newtonsoft.Json;
using Riskified.SDK.Model.OrderElements;

namespace Riskified.SDK.Tests;

public class ModelSerializationTests
{
    [Fact]
    public void Customer_Serializes_WithSnakeCase()
    {
        var customer = new Customer("John", "Doe", "123", 1, "john@example.com", true, System.DateTime.UtcNow);
        var json = JsonConvert.SerializeObject(customer);
        
        Assert.Contains("\"first_name\"", json);
        Assert.Contains("\"last_name\"", json);
    }

    [Fact]
    public void LineItem_RoundTrip()
    {
        var item = new LineItem("Product", 99.99, 2, "prod-123", "SKU-123");
        var json = JsonConvert.SerializeObject(item);
        var result = JsonConvert.DeserializeObject<LineItem>(json);
        
        Assert.Equal(item.Title, result.Title);
        Assert.Equal(item.Price, result.Price);
    }
}
