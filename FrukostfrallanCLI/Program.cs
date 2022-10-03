// See https://aka.ms/new-console-template for more information



using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace FrukostFrallanCLI
{
	class Program
	{
		private static string tokenGenUrl = "https://www.wix.com/installer/install?appId=27b14e39-c86f-49b0-b8b5-856238332649&redirectUrl=https://example.com";
		private static string clientId = "27b14e39-c86f-49b0-b8b5-856238332649";
		private static string clientSecret = "b265fee0-475c-41c5-8398-8557353f4503";
		private static string token = "OAUTH2.eyJraWQiOiJWUTQwMVRlWiIsImFsZyI6IkhTMjU2In0.eyJkYXRhIjoie1wiYXBwSWRcIjpcIjI3YjE0ZTM5LWM4NmYtNDliMC1iOGI1LTg1NjIzODMzMjY0OVwiLFwiaW5zdGFuY2VJZFwiOlwiNDA0YjkxY2UtOWMwNy00OTM5LWExNTktMzBhMjMwZjg4Yjg4XCIsXCJzY29wZVwiOltcIlNDT1BFLkRDLVNUT1JFUy1NRUdBLk1BTkFHRS1TVE9SRVNcIixcIlNDT1BFLkRDLk1BTkFHRS1ZT1VSLUFQUFwiXX0iLCJpYXQiOjE2NjQ3OTQyMzMsImV4cCI6MTY2NDc5NDgzM30.Gn5pyGFnCOMIwvSGEsgcoMQDo2m7GeF5Ahd6uqKJxnw";
		private static AuthorizationData? AuthorizationData;
		private static DateTime NextSaturday = default(DateTime);
		private static int Week = 0;
		private static string RootFolder = @"F:\OneDrive\Dokument\Privat\Frukostfrallan";
		private static string WeekFolder = @"";
		private static string PackingSlipFolder = @"";
		private static readonly HttpClient _httpClient = new HttpClient();

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine($"Invalid args. Need a token from");
				Console.WriteLine(tokenGenUrl);
				return;
			}

			var command = args[0];

			switch (command)
			{
				case "prep":
					token = args[1].ToString();

					PrepDelivery();

					break;
				case "sortorders":

				//	var csvFile2 = args[1];
				//	var googleUrl = args[2];
				//	SortOrders(csvFile2, googleUrl);
				//	break;
				default:
					Console.WriteLine("ERROR: Invalid command");
					break;
			}



			Console.ReadKey();
		}

		private static void PrepDelivery()
		{
			SetDefaultValues();

			Console.WriteLine("Start prep the delivery");

			// Check if the root folder exist. If not.. there maybe the wrong computer. Exit and give warning
			if (!Directory.Exists(RootFolder))
			{
				Console.WriteLine($"ERROR: RootFolder to Frukostfrallan {RootFolder} does not exist. Cancel prep.");
				return;
			}
			if (!Directory.Exists(WeekFolder))
			{
				Directory.CreateDirectory(WeekFolder);
				Console.WriteLine($"WeekFolder created: {WeekFolder}");
			}
			if (!Directory.Exists(PackingSlipFolder))
			{
				Directory.CreateDirectory(PackingSlipFolder);
				Console.WriteLine($"PackingSlipFolder created: {PackingSlipFolder}");
			}


			Authorize();
			var orders = ListNotFullfilledOrders();

			foreach (var order in orders)
			{
				DownloadPackingSlip(order);
			}

			//var ordersDeliveryOrder = SortOrders(orders);

			CreateGoogleMapsFile(orders);

			CreateOrderMailFile();

			CreateOrderJsonFile(orders);
		}

		private static void SetDefaultValues()
		{
			NextSaturday = GetNextSaturday();
			Console.WriteLine($"Next saturday: {NextSaturday.ToString("yyyy-MM-dd")}");
			Week = System.Globalization.ISOWeek.GetWeekOfYear(NextSaturday);
			Console.WriteLine($"Week: {Week}");
			Console.WriteLine($"RootFolder: {RootFolder}");
			WeekFolder = $"{RootFolder}\\{NextSaturday.ToString("yyyy")}v{Week}";
			Console.WriteLine($"WeekFolder: {WeekFolder}");
			PackingSlipFolder = $"{WeekFolder}\\packingSlips";
			Console.WriteLine($"PackingSlipsFolder: {PackingSlipFolder}");

		}

		private static void Authorize()
		{
			var url = "https://www.wix.com/oauth/access";

			var client = new RestClient(url);
			client.AddDefaultHeader("Accept", "application/json;");

			var body = new AuthorizationRequestBody { GrantType = "authorization_code", ClientId = clientId, ClientSecret = clientSecret, Code = token };

			var request = new RestRequest(url);
			request.AddStringBody(JsonConvert.SerializeObject(body), "application/json");
			request.Method = Method.Post;

			Console.WriteLine("Will try to authorize");
			try
			{
				var response = client.Post(request);

				if (response == null || response.Content == null)
				{
					Console.WriteLine("Access denied");
					return;
				}

				AuthorizationData = JsonConvert.DeserializeObject<AuthorizationData>(response.Content.ToString());

				if (AuthorizationData != null)
				{
					Console.WriteLine("Got access_token"); // + AuthorizationData.AccessToken);
														   //Console.WriteLine("refresh_token: " + AuthorizationData.RefreshToken);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Access denied. Rerun with new token."); // Ex: {ex.Message}");
				Console.WriteLine($"{tokenGenUrl}"); // Ex: {ex.Message}");
				return;
			}

		}

		private static List<Order> ListNotFullfilledOrders()
		{
			var orders = new List<Order>();

			var url = "https://www.wixapis.com/stores/v2/orders/query";

			var client = new RestClient(url);
			client.AddDefaultHeader("Accept", "application/json;");
			client.AddDefaultHeader("Authorization", AuthorizationData.AccessToken);

			var request = new RestRequest();
			var crappyJson = "{\"query\":{\"paging\":{\"limit\":100,\"offset\":0},\"filter\":\"{\\\"fulfillmentStatus\\\": \\\"NOT_FULFILLED\\\"}\"}}";
			Console.WriteLine(crappyJson);
			request.AddStringBody(crappyJson, "application/json");
			request.Method = Method.Post;

			Console.WriteLine("Will try to query not fullfiled orders");
			//try
			//{
				var response = client.Post(request);



			if (response == null)
			{
				Console.WriteLine("No orders");
			}
			else
			{
				Console.WriteLine(response.Content);
				var responseOrders = JsonConvert.DeserializeObject<OrderQueryResult>(response.Content);
				if (responseOrders != null)
				{
					Console.WriteLine($"Found {responseOrders.TotalResults} orders.");
					orders.AddRange(responseOrders.Orders);
				}
			}
			return orders;
		}

		private static DateTime GetNextSaturday()
		{
			var nextSaturday = default(DateTime);

			var playDate = DateTime.Now;

			for (int i = 1; i < 7; i++)
			{
				if (playDate.AddDays(i).DayOfWeek == DayOfWeek.Saturday)
				{
					nextSaturday = playDate.AddDays(i);
					break;

				}
			}

			return nextSaturday;
		}

		private static void CreateOrderMailFile()
		{
			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_BeställningsMail.txt";

			if (File.Exists(path))
			{
				File.Delete(path);
				Console.WriteLine($"Deleted file {path}.");
			}

			
			using (StreamWriter sw = File.CreateText(path))
			{
				sw.WriteLine($"Frukostfrallan beställning v{Week} ({NextSaturday.ToString("yyyy-MM-dd")})");
				sw.WriteLine("");
				sw.WriteLine("Hej Ahmet");
				sw.WriteLine("");
				sw.WriteLine("Följande produkter vill vi beställa. Om ni har några frågetecken maila eller ring så läser vi det.");
				sw.WriteLine("");
				sw.WriteLine("[ProduktTabell]");
				sw.WriteLine("");
				sw.WriteLine("Bekräfta gärna via mail eller sms att ni har mottagit beställningen.");
				sw.WriteLine("");
				sw.WriteLine("Mvh");
				sw.WriteLine("Måns och Moa (Frukostfrallan)");

				Console.WriteLine($"Created file {path}");

			}
		}

		private static void DownloadPackingSlip(Order order)
		{
			var url = $"https://www.wixapis.com/stores/v2/orders/packingSlip/{order.Id}";

			var client = new RestClient(url);
			client.AddDefaultHeader("Accept", "application/json;");
			client.AddDefaultHeader("Authorization", AuthorizationData.AccessToken);

			var request = new RestRequest();
			Console.WriteLine("Will try to query not fullfiled orders");
			//try
			//{
			var response = client.Get(new RestRequest());

			if (response == null)
			{
				Console.WriteLine("No packingSlip");
			}
			else
			{
				Console.WriteLine(response.Content);
				var responsePackingSlip = JsonConvert.DeserializeObject<PackingSlip>(response.Content);
				Console.WriteLine($"PackingSlip link: {responsePackingSlip.Link}");

				var path = $"{PackingSlipFolder}\\{order.Number}.pdf";

				var download = DownloadFileAsync(responsePackingSlip.Link, path);
				// Wait for the delay to complete.
				download.Wait();
			}
		}

		private static async Task DownloadFileAsync(string uri, string outputPath)
		{
			Uri uriResult;

			if (!Uri.TryCreate(uri, UriKind.Absolute, out uriResult))
				throw new InvalidOperationException("URI is invalid.");

			//if (!File.Exists(outputPath))
			//	throw new FileNotFoundException("File not found."
			//	   , nameof(outputPath));

			byte[] fileBytes = await _httpClient.GetByteArrayAsync(uri);
			File.WriteAllBytes(outputPath, fileBytes);
		}

		private static void CreateGoogleMapsFile(List<Order> orders)
		{
			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_GoogleMaps.txt";

			if (File.Exists(path))
			{
				File.Delete(path);
				Console.WriteLine($"Deleted file {path}.");
			}


			using (StreamWriter sw = File.CreateText(path))
			{
				sw.WriteLine("Map (1): XX min");
				sw.WriteLine("https://www.google.com/maps/dir/Skälby+Gårds+väg+6,+Järfälla");

				foreach (var order in orders)
				{
					sw.WriteLine($",+Järfälla/{order.ShippingInfo.ShipmentDetails.Address.AddressLine1}");
				}

				Console.WriteLine($"Created file {path}");

			}
		}

		private static void CreateDeliveryOrderFile(List<Order> orders)
		{
			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_DeliveryOrder.json";

			if (File.Exists(path))
			{
				File.Delete(path);
				Console.WriteLine($"Deleted file {path}.");
			}


			using (StreamWriter sw = File.CreateText(path))
			{
				sw.WriteLine("");

				foreach (var order in orders)
				{
					sw.WriteLine($"{order.Number}, {order.ShippingInfo.ShipmentDetails.Address.AddressLine1}, {order.ShippingInfo.ShipmentDetails.Address.ZipCode}");
				}

				Console.WriteLine($"Created file {path}");

			}
		}

		private static void CreateOrderJsonFile(List<Order> orders)
		{

			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_Orders.txt";

			if (File.Exists(path))
			{
				File.Delete(path);
				Console.WriteLine($"Deleted file {path}.");
			}

			string json = JsonConvert.SerializeObject(orders);
			File.WriteAllText(path, json);

			Console.WriteLine($"Created file {path}");
		}

		//private static void SortOrders(List<Order> orders) { 

		//}

		//private static void SortOrders(List<Order> orders)
		//{
		//	var ordersToSort = new List<Order>();
		//	ordersToSort.AddRange(orders);
		//	var group = new List<Order>();

		//	group = HandlePostalArea(group, ordersToSort, "17561");
		//	group = HandlePostalArea(group, ordersToSort, "17562");
		//	group = HandlePostalArea(group, ordersToSort, "17563");
		//	group = HandlePostalArea(group, ordersToSort, "17746");
		//	group = HandlePostalArea(group, ordersToSort, "17569");
		//	group = HandlePostalArea(group, ordersToSort, "17564");
		//	group = HandlePostalArea(group, ordersToSort, "17567");
		//	group = HandlePostalArea(group, ordersToSort, "16247");
		//	group = HandlePostalArea(group, ordersToSort, "17560");
		//	group = HandlePostalArea(group, ordersToSort, "17565");
		//	group = HandlePostalArea(group, ordersToSort, "17566");
		//	group = HandlePostalArea(group, ordersToSort, "17568");
		//	if (ordersToSort.Any())
		//	{
		//		group.AddRange(ordersToSort);
		//	}

		//	if (group.Any())
		//	{
		//		//PrintGoogleMapUrl(group);
		//		group.Clear();
		//	}
		//}

		//private static List<Address> HandlePostalArea(List<Order> group, List<Order> ordersToSort, string postalNumber)
		//{
		//	var postalAddresses = ordersToSort.Where(x => x.ZipCode == postalNumber).ToList();
		//	if (postalAddresses.Any())
		//	{
		//		if ((group.Count + postalAddresses.Count()) < 20)
		//		{
		//			group.AddRange(postalAddresses);
		//		}
		//		else
		//		{
		//			PrintGoogleMapUrl(group);
		//			group.Clear();
		//			group.AddRange(postalAddresses);
		//		}

		//		foreach (var postalAddress in postalAddresses)
		//		{
		//			ordersToSort.Remove(postalAddress);
		//		}

		//	}

		//	return group;
		//}
	}

	public class PackingSlip
	{
		public PackingSlip()
		{
			Link = string.Empty;
		}

		[JsonProperty(PropertyName = "link")]
		public string Link { get; set; }
	}

	public class AuthorizationRequestBody
	{
		public AuthorizationRequestBody()
		{
			GrantType = string.Empty;
			ClientId = string.Empty;
			ClientSecret = string.Empty;
			Code = string.Empty;
		}

		[JsonProperty(PropertyName = "grant_type")]
		public string GrantType { get; set; }

		[JsonProperty(PropertyName = "client_id")]
		public string ClientId { get; set; }

		[JsonProperty(PropertyName = "client_secret")]
		public string ClientSecret { get; set; }

		[JsonProperty(PropertyName = "code")]
		public string Code { get; set; }
	}

	public class AuthorizationData
	{
		public AuthorizationData()
		{
			AccessToken = string.Empty;
			RefreshToken = string.Empty;
		}

		[JsonProperty(PropertyName = "access_token")]
		public string AccessToken { get; set; }

		[JsonProperty(PropertyName = "refresh_token")]
		public string RefreshToken { get; set; }
	}

	public class OrderQueryResult
	{
		public OrderQueryResult()
		{
			TotalResults = 0;
		}

		[JsonProperty(PropertyName = "orders")]
		public List<Order> Orders { get; set; }

		[JsonProperty(PropertyName = "metadata")]
		public MetaData? Metadata { get; set; }

		[JsonProperty(PropertyName = "totalResults")]
		public int TotalResults { get; set; }
	}

	public class MetaData
	{
		public MetaData()
		{
			Items = 0;
			Offset = 0;
		}

		[JsonProperty(PropertyName = "items")]
		public int Items { get; set; }

		[JsonProperty(PropertyName = "offset")]
		public int Offset { get; set; }
	}

	public class Order
	{
		public Order()
		{
			Id = string.Empty;
			Number = 0;
			PaymentStatus = string.Empty;
			FulfillmentStatus = string.Empty;

			ShippingInfo = new ShippingInfo();

			//AddressLine1 = string.Empty;
			//AddressLine2 = string.Empty;
			//ZipCode = string.Empty;
			//City = string.Empty;
			//Phone = string.Empty;
		}

		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }

		[JsonProperty(PropertyName = "number")]
		public int Number { get; set; }

		[JsonProperty(PropertyName = "totals")]
		public Totals? Totals { get; set; }

		[JsonProperty(PropertyName = "paymentStatus")]
		public string PaymentStatus { get; set; }

		[JsonProperty(PropertyName = "fulfillmentStatus")]
		public string FulfillmentStatus { get; set; }

		[JsonProperty(PropertyName = "shippingInfo")]
		public ShippingInfo ShippingInfo { get; set; }

		//[JsonIgnore]
		//public string AddressLine1
		//{
		//	get => ShippingInfo.ShipmentDetails.Address.AddressLine1;
		//	set => AddressLine1 = value;
		//}
		//[JsonIgnore]
		//public string AddressLine2 
		//{ 
		//	get => ShippingInfo.ShipmentDetails.Address.AddressLine2; 
		//	set => AddressLine2 = value; 
		//}
		//[JsonIgnore]
		//public string ZipCode 
		//{
		//	get => ShippingInfo.ShipmentDetails.Address.ZipCode; 
		//	set => ZipCode = value; 
		//}
		//[JsonIgnore]
		//public string City 
		//{ 
		//	get => ShippingInfo.ShipmentDetails.Address.City; 
		//	set => City = value; 
		//}
		//[JsonIgnore]
		//public string Phone
		//{
		//	get => ShippingInfo.ShipmentDetails.Address.Phone; 
		//	set => Phone = value;
		//}
	}

	public class Totals
	{
		public Totals()
		{
			Total = string.Empty;
			Quantity = string.Empty;
		}

		[JsonProperty(PropertyName = "total")]
		public string Total { get; set; }

		[JsonIgnore]
		public int TotalSum { get => Convert.ToInt32(Total.Replace(".0", "")); }

		[JsonProperty(PropertyName = "quantity")]
		public string Quantity { get; set; }

		[JsonIgnore]
		public int QuantitySum { get => Convert.ToInt32(Quantity.Replace(".0", "")); }

	}

	public class ShippingInfo
	{
		public ShippingInfo()
		{
			ShipmentDetails = new ShipmentDetails();
		}

		[JsonProperty(PropertyName = "shipmentDetails")]
		public ShipmentDetails ShipmentDetails { get; set; }

	}

	public class ShipmentDetails
	{
		public ShipmentDetails()
		{
			Address = new Address();
		}

		[JsonProperty(PropertyName = "address")]
		public Address Address { get; set; }
	}

	public class Address
	{
		public Address()
		{
			AddressLine1 = string.Empty;
			AddressLine2 = string.Empty;
			ZipCode = string.Empty;
			City = string.Empty;
			Phone = string.Empty;
		}

		[JsonProperty(PropertyName = "addressLine1")]
		public string AddressLine1 { get; set; }

		[JsonProperty(PropertyName = "addressLine2")]
		public string AddressLine2 { get; set; }

		[JsonProperty(PropertyName = "zipCode")]
		public string ZipCode { get; set; }

		[JsonProperty(PropertyName = "city")]
		public string City { get; set; }

		[JsonProperty(PropertyName = "Phone")]
		public string Phone { get; set; }
	}
}