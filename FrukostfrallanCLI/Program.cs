// See https://aka.ms/new-console-template for more information



using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

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
		private static string DataFolder = @"";
		private static readonly HttpClient _httpClient = new HttpClient();
		private static bool NoDownload = false;
		private static bool NoPrintPdf = false;
		private static bool PrintVerbose = false;
		private static CollectionQueryResult Collections = new CollectionQueryResult();

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine($"Invalid args. -prep/-sort");
				return;
			}

			var command = args[0];

			SetDefaultValues();
			CreateFolders();

			switch (command)
			{
				case "-prep":
					if (args.Length <= 1)
					{
						Console.WriteLine($"Invalid args. Missing token. ex: .\\FrukostfrallanCLI.exe -prep OAUTH2.eyJ...");
						return;
					}

					token = args[1].ToString();

					if (args.Any("-nodownload".Contains)) {
						NoDownload = true;
					}

					PrepDelivery();

					break;
				case "-sort":
					if (args.Any("-noprintpdf".Contains))
					{
						NoPrintPdf = true;
					}
					SortDelivery();
					break;
				default:
					Console.WriteLine("ERROR: Invalid command");
					break;
			}


			Console.WriteLine("");
			Console.WriteLine("Press any key to exit....");
			Console.ReadKey();
		}

		private static void CreateFolders()
		{
			// Check if the root folder exist. If not.. there maybe the wrong computer. Exit and give warning
			if (!Directory.Exists(RootFolder))
			{
				Console.WriteLine($"ERROR: RootFolder to Frukostfrallan {RootFolder} does not exist. Cancel prep.");
				return;
			}
			CreateFolder(WeekFolder, "WeekFolder");
			CreateFolder(PackingSlipFolder, "PackingSlipFolder");
			CreateFolder(DataFolder, "DataFolder");
		}

		private static void CreateFolder(string path, string name)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
				PrintVerboseConsole($"{name} created: {path}");
			}
		}

		private static void PrepDelivery()
		{
			Console.WriteLine("Start prep the delivery");

			Authorize();
			var orders = ListNotFullfilledOrders();
			GetCollections();

			var bakeryOrder = new BakeryOrder();

			foreach (var order in orders)
			{
				if (!NoDownload) { DownloadPackingSlip(order); }
				bakeryOrder = PopulateBakeryOrder(bakeryOrder, order);
			}

			CreateJsonFile("BakeryOrder", JsonConvert.SerializeObject(bakeryOrder));
			CreateBakeryOrderTextFile(bakeryOrder);

			CreateJsonFile("Orders", JsonConvert.SerializeObject(orders));

			var mapSortedOrders = MapSortOrders(orders);
			CreateGoogleMapsFile(mapSortedOrders);

			CreateBakeryOrderMailFile(bakeryOrder);

			PrintVerboseConsole("--** Jobs done **--");
		}

		private static void SortDelivery()
		{
			Console.WriteLine("Start sort the delivery");

			var orderJsonPath = $"{DataFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_Orders.json";
			string orderJson = System.IO.File.ReadAllText(orderJsonPath);

			var mapsOrderPath = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_GoogleMaps.txt";
			string[] mapsOrder = System.IO.File.ReadAllLines(mapsOrderPath);

			if (string.IsNullOrEmpty(orderJson) || mapsOrder.Length == 0)
			{
				if (string.IsNullOrEmpty(orderJson))
				{
					Console.WriteLine($"Could not find/read order json file: {orderJsonPath}");
				}
				if (mapsOrder.Length == 0)
				{
					Console.WriteLine($"Could not find/read order json file: {mapsOrderPath}");
				}

				return;
			}

			var sortOrders = new List<Order>();
			var orders = JsonConvert.DeserializeObject<List<Order>>(orderJson);
			if (orders == null)
			{
				Console.WriteLine($"Could not deserialize: {orderJsonPath}");
			}

			foreach (var line in mapsOrder)
			{
				if (line.StartsWith(",+Järfälla/"))
				{
					var addressLine1 = line.Replace(",+Järfälla/", "");
					var order = orders.First(x => x.ShippingInfo.ShipmentDetails.Address.AddressLine1 == addressLine1);
					sortOrders.Add(order);
				}
				else if (string.IsNullOrEmpty(line))
				{
					var emptyOrder = new Order();
					sortOrders.Add(emptyOrder);
				}
			}

			CreateDeliveryOrderFile(sortOrders);

			if (!NoPrintPdf)
			{
				foreach (var order in sortOrders)
				{
					var pdfPath = $"{PackingSlipFolder}\\{order.Number}.pdf";
					OpenUrl(pdfPath);
				}
			}
			else
			{
				PrintVerboseConsole("NoPrintPdf: No PDFs are opened.");
			}

			PrintVerboseConsole("--** Jobs done **--");
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
			PrintVerboseConsole($"PackingSlipsFolder: {PackingSlipFolder}");
			DataFolder = $"{WeekFolder}\\data";
			PrintVerboseConsole($"DataFolder: {DataFolder}");

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

			PrintVerboseConsole("Will try to authorize");
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
					PrintVerboseConsole("Got access_token"); // + AuthorizationData.AccessToken);
														   //Console.WriteLine("refresh_token: " + AuthorizationData.RefreshToken);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Access denied. Rerun with new token."); // Ex: {ex.Message}");
				Console.WriteLine($"{tokenGenUrl}"); // Ex: {ex.Message}");
				OpenUrl(tokenGenUrl);
				return;
			}

		}

		private static void OpenUrl(string url)
		{
			try
			{
				Process.Start(url);
			}
			catch
			{
				// hack because of this: https://github.com/dotnet/corefx/issues/10361
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					Process.Start("xdg-open", url);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", url);
				}
				else
				{
					throw;
				}
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
			PrintVerboseConsole(crappyJson);
			request.AddStringBody(crappyJson, "application/json");
			request.Method = Method.Post;

			PrintVerboseConsole("Will try to query not fullfiled orders");
			//try
			//{
				var response = client.Post(request);



			if (response == null)
			{
				Console.WriteLine("No orders");
			}
			else
			{
				PrintVerboseConsole(response.Content);
				CreateJsonFile("WixOrders", response.Content);
				var responseOrders = JsonConvert.DeserializeObject<OrderQueryResult>(response.Content);
				if (responseOrders != null)
				{
					Console.WriteLine($"Found {responseOrders.TotalResults} orders.");
					orders.AddRange(responseOrders.Orders);
				}
			}
			return orders;
		}

		private static void CreateJsonFile(string name, string content)
		{
			var path = $"{DataFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_{name}.json";

			if (File.Exists(path))
			{
				File.Delete(path);
				PrintVerboseConsole($"Deleted file {path}.");
			}

			File.WriteAllText(path, content);

			PrintVerboseConsole($"Created file {path}");
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

		private static void CreateBakeryOrderMailFile(BakeryOrder bakeryOrder)
		{
			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_BeställningsMail.txt";

			if (File.Exists(path))
			{
				File.Delete(path);
				PrintVerboseConsole($"Deleted file {path}.");
			}
			
			using (StreamWriter sw = File.CreateText(path))
			{
				sw.WriteLine($"Frukostfrallan beställning v{Week} ({NextSaturday.ToString("yyyy-MM-dd")})");
				sw.WriteLine("");
				sw.WriteLine("Hej Ahmet");
				sw.WriteLine("");
				sw.WriteLine("Följande produkter vill vi beställa. Om ni har några frågetecken maila eller ring så läser vi det.");
				sw.WriteLine("");
				sw.WriteLine("-----------------------");
				foreach (var line in bakeryOrder.BakeryOrderLines.Where(x => x.BakeryProduct).ToList())
				{
					sw.WriteLine($"{line.QuantityString} st {line.Name}");
				}
				sw.WriteLine("-----------------------");
				sw.WriteLine("");
				sw.WriteLine("-------Kontrollera och tabort----------");
				foreach (var line in bakeryOrder.BakeryOrderLines.Where(x => !x.BakeryProduct).ToList())
				{
					sw.WriteLine($"{line.QuantityString} st {line.Name}");
				}
				sw.WriteLine("-------Kontrollera och tabort----------");
				sw.WriteLine("");
				sw.WriteLine("Bekräfta gärna via mail eller sms att ni har mottagit beställningen.");
				sw.WriteLine("");
				sw.WriteLine("Mvh");
				sw.WriteLine("Måns och Moa (Frukostfrallan)");

				PrintVerboseConsole($"Created file {path}");

			}
		}

		private static void CreateBakeryOrderTextFile(BakeryOrder bakeryOrder)
		{
			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_Beställningar.csv";

			if (File.Exists(path))
			{
				File.Delete(path);
				PrintVerboseConsole($"Deleted file {path}.");
			}


			using (StreamWriter sw = File.CreateText(path))
			{
				foreach (var line in bakeryOrder.BakeryOrderLines.ToList())
				{
					sw.WriteLine($"{line.Name}; {line.Quantity}; {line.TotalPrice}; {line.Price}; {line.BakeryProduct}");
				}

				PrintVerboseConsole($"Created file {path}");

			}
		}

		private static void DownloadPackingSlip(Order order)
		{
			var url = $"https://www.wixapis.com/stores/v2/orders/packingSlip/{order.Id}";

			var client = new RestClient(url);
			client.AddDefaultHeader("Accept", "application/json;");
			client.AddDefaultHeader("Authorization", AuthorizationData.AccessToken);

			PrintVerboseConsole("Will try to get packingSlip");
			var response = client.Get(new RestRequest());

			if (response == null)
			{
				Console.WriteLine("No packingSlip");
			}
			else
			{
				//PrintVerboseConsole(response.Content);
				var responsePackingSlip = JsonConvert.DeserializeObject<PackingSlip>(response.Content);
				PrintVerboseConsole($"PackingSlip link: {responsePackingSlip.Link}");

				var path = $"{PackingSlipFolder}\\{order.Number}.pdf";

				var download = DownloadFileAsync(responsePackingSlip.Link, path);
				// Wait for the delay to complete.
				download.Wait();
			}
			Console.Write(".");
		}

		private static Product GetProduct(string productId)
		{
			var product = default(Product);
			var url = $"https://www.wixapis.com/stores/v1/products/{productId}";

			var client = new RestClient(url);
			client.AddDefaultHeader("Accept", "application/json;");
			client.AddDefaultHeader("Authorization", AuthorizationData.AccessToken);

			PrintVerboseConsole($"Will try to get product {productId}");
			var response = client.Get(new RestRequest());

			if (response == null)
			{
				Console.WriteLine("No product");
			}
			else
			{
				//PrintVerboseConsole(response.Content);
				CreateJsonFile($"product_{productId}", response.Content);
				product = JsonConvert.DeserializeObject<Product>(response.Content);

				if (product != null)
				{
					foreach (var collection in Collections.Collections)
					{
						if (collection != null && response.Content.Contains(collection.Id))
						{
							product.Collections.Add(collection);
						}
					}
				}
			}

			Console.Write(".");

			return product;
		}

		private static void GetCollections()
		{
			var url = "https://www.wixapis.com/stores/v1/collections/query";

			var client = new RestClient(url);
			client.AddDefaultHeader("Accept", "application/json;");
			client.AddDefaultHeader("Authorization", AuthorizationData.AccessToken);

			var request = new RestRequest();
			var crappyJson = "{\"query\":{\"paging\":{\"limit\":100,\"offset\":0}}}";
			//PrintVerboseConsole(crappyJson);
			request.AddStringBody(crappyJson, "application/json");
			request.Method = Method.Post;
			PrintVerboseConsole("Will try to query collections");
			var response = client.Post(request);

			if (response == null)
			{
				Console.WriteLine("No collections");
			}
			else
			{
				//PrintVerboseConsole(response.Content);
				CreateJsonFile("WixCollections", response.Content);
				Collections = JsonConvert.DeserializeObject<CollectionQueryResult>(response.Content);
			}
			Console.Write(".");
		}

		private static async Task DownloadFileAsync(string uri, string outputPath)
		{
			Uri uriResult;

			if (!Uri.TryCreate(uri, UriKind.Absolute, out uriResult))
				throw new InvalidOperationException("URI is invalid.");

			byte[] fileBytes = await _httpClient.GetByteArrayAsync(uri);
			File.WriteAllBytes(outputPath, fileBytes);
		}

		private static void CreateGoogleMapsFile(List<Order> orders)
		{
			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_GoogleMaps.txt";

			if (File.Exists(path))
			{
				File.Delete(path);
				PrintVerboseConsole($"Deleted file {path}.");
			}

			using (StreamWriter sw = File.CreateText(path))
			{
				sw.WriteLine("Map (1): XX min");
				sw.WriteLine("https://www.google.com/maps/dir/Skälby+Gårds+väg+6,+Järfälla");

				foreach (var order in orders)
				{
					sw.WriteLine($",+Järfälla/{order.ShippingInfo.ShipmentDetails.Address.AddressLine1}");
				}

				PrintVerboseConsole($"Created file {path}");

			}
		}

		private static void CreateDeliveryOrderFile(List<Order> orders)
		{
			var path = $"{WeekFolder}\\{NextSaturday.ToString("yyyy")}v{Week}_DeliveryOrder.txt";

			if (File.Exists(path))
			{
				File.Delete(path);
				PrintVerboseConsole($"Deleted file {path}.");
			}


			using (StreamWriter sw = File.CreateText(path))
			{
				sw.WriteLine("");

				foreach (var order in orders)
				{
					sw.WriteLine($"{order.Number}, {order.ShippingInfo.ShipmentDetails.Address.AddressLine1}, {order.ShippingInfo.ShipmentDetails.Address.ZipCode}");
				}

				PrintVerboseConsole($"Created file {path}");

			}
		}

		private static BakeryOrder PopulateBakeryOrder(BakeryOrder bakeryOrder, Order order)
		{
			foreach (var lineItem in order.LineItems)
			{
				var line = bakeryOrder.BakeryOrderLines.FirstOrDefault(x => x.Name == lineItem.Name);
				if (line == null)
				{
					line = new BakeryOrderLine { 
						Name = lineItem.Name, 
						Price = lineItem.PriceSum,  
						Quantity = lineItem.Quantity,
						TotalPrice = lineItem.TotalPriceSum
					};

					var product = GetProduct(lineItem.ProductId);
					if (product != null)
					{
						if (product.Collections.Any(x => x.Name == "Bröd" || x.Name == "Fikabröd" || x.Name == "Frukostfralla" || x.Name == "Limpor" || x.Name == "Längd"))
						{
							line.BakeryProduct = true;
						}
					}

					bakeryOrder.BakeryOrderLines.Add(line);
				} 
				else
				{
					line.Quantity = line.Quantity + lineItem.Quantity;
					line.TotalPrice = line.TotalPrice + lineItem.TotalPriceSum;
				}
			}

			return bakeryOrder;
		}

		private static List<Order> MapSortOrders(List<Order> orders)
		{
			var mapSortedOrders = new List<Order>();

			HandlePostalArea(mapSortedOrders, orders, "17562");
			HandlePostalArea(mapSortedOrders, orders, "17563");
			HandlePostalArea(mapSortedOrders, orders, "17746");
			HandlePostalArea(mapSortedOrders, orders, "17569");
			HandlePostalArea(mapSortedOrders, orders, "17564");
			HandlePostalArea(mapSortedOrders, orders, "17567");
			HandlePostalArea(mapSortedOrders, orders, "16247");
			HandlePostalArea(mapSortedOrders, orders, "17560");
			HandlePostalArea(mapSortedOrders, orders, "17565");
			HandlePostalArea(mapSortedOrders, orders, "17566");
			HandlePostalArea(mapSortedOrders, orders, "17568");
			HandlePostalArea(mapSortedOrders, orders, "17561");

			if (mapSortedOrders.Count > 20)
			{
				mapSortedOrders = HandleSplitDelivery(mapSortedOrders);
			}

			return mapSortedOrders; 
		}

		private static void HandlePostalArea(List<Order> mapSortedOrders, List<Order> orders, string postalNumber)
		{
			var postalAddresses = orders.Where(x => x.ShippingInfo.ShipmentDetails.Address.ZipCode.Replace(" ", "") == postalNumber.Replace(" ", "")).ToList();
			if (postalAddresses.Any())
			{
				mapSortedOrders.AddRange(postalAddresses);

				foreach (var postalAddress in postalAddresses)
				{
					orders.Remove(postalAddress);
					Console.Write(".");
				}
			}
		}

		private static List<Order> HandleSplitDelivery(List<Order> orders)
		{
			var mapSortedOrders = new List<Order>();

			var itterator = 1;
			foreach (var order in orders)
			{
				mapSortedOrders.Add(order);

				if (itterator == 20)
				{
					mapSortedOrders.Add(new Order());
					itterator = 0;
				}
				itterator++;
				Console.Write(".");
			}

			return mapSortedOrders;
		}

		private static void PrintVerboseConsole(string line)
		{
			if (PrintVerbose)
			{
				Console.WriteLine(line);
			}
		}
	}

	public class CollectionQueryResult
	{
		public CollectionQueryResult()
		{
			TotalResults = 0;
		}

		[JsonProperty(PropertyName = "collections")]
		public List<Collection> Collections { get; set; }

		[JsonProperty(PropertyName = "metadata")]
		public MetaData? Metadata { get; set; }

		[JsonProperty(PropertyName = "totalResults")]
		public int TotalResults { get; set; }
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
			LineItems = new List<LineItem>();
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

		[JsonProperty(PropertyName = "lineItems")]
		public List<LineItem> LineItems { get; set; }

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

	public class LineItem
	{
		public LineItem()
		{
			Index = 0;
			Quantity = 0;
			Price = string.Empty;
			TotalPrice = string.Empty;
			Name = string.Empty;
			ProductId = string.Empty;
		}

		[JsonProperty(PropertyName = "index")]
		public int Index { get; set; }

		[JsonProperty(PropertyName = "quantity")]
		public int Quantity { get; set; }

		[JsonProperty(PropertyName = "price")]
		public string Price { get; set; }

		[JsonIgnore]
		public int PriceSum { get => Convert.ToInt32(Price.Replace(".00", "")); }

		[JsonProperty(PropertyName = "totalPrice")]
		public string TotalPrice { get; set; }

		[JsonIgnore]
		public int TotalPriceSum { get => Convert.ToInt32(TotalPrice.Replace(".00", "")); }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "productId")]
		public string ProductId { get; set; }
	}

	public class BakeryOrder
	{
		public BakeryOrder()
		{
			BakeryOrderLines = new List<BakeryOrderLine>();
		}

		public List<BakeryOrderLine> BakeryOrderLines { get; set; }

		public int NumberOfOrders { get => BakeryOrderLines.Count; }
		public int TotalQuantity { get => BakeryOrderLines.Sum(x => x.Quantity); }
		public int TotalPrice { get => BakeryOrderLines.Sum(x => x.TotalPrice); }

	}

	public class BakeryOrderLine
	{
		public BakeryOrderLine()
		{
			Quantity = 0;
			Price = 0;
			TotalPrice = 0;
			Name = string.Empty;
		}

		public int Quantity { get; set; }

		[JsonIgnore]
		public string QuantityString 
		{ 
			get {
				var result = string.Empty;

				if (Quantity < 10)
				{
					result = $"  {Quantity}";
				} else if (Quantity > 10 && Quantity < 100)
				{
					result = $" {Quantity}";
				} else
				{
					result = $"{Quantity}";
				}

				return result;
			} 
		}

		public int Price { get; set; }

		public int TotalPrice { get; set; }

		public string Name { get; set; }

		public bool BakeryProduct { get; set; }
	}

	public class Product
	{
		public Product()
		{
			Id = string.Empty;
			Name = string.Empty;
			Slug = string.Empty;
			Ribbon = string.Empty;
			Brand = string.Empty;

			Collections = new List<Collection>();
		}

		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "slug")]
		public string Slug { get; set; }

		[JsonProperty(PropertyName = "ribbon")]
		public string Ribbon { get; set; }

		[JsonProperty(PropertyName = "brand")]
		public string Brand { get; set; }

		[JsonProperty(PropertyName = "collectionIds")]
		public IList<string> collectionIds { get; set; }

		[JsonIgnore]
		public List<Collection> Collections { get; set; }

		//"customTextFields": [  {
		//	"title": "What would you like us to print on the custom label?",
		//	"maxLength": 200,
		//	"mandatory": false
		//  }],
	}

	public class Collection
	{
		public Collection()
		{
			Id = string.Empty;
			Name = string.Empty;
		}

		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }
	}
}