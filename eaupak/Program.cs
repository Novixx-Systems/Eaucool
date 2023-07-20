using System.Net;

namespace eaupak
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: eaupak <package name>");
                return;
            }
            // Get the package name
            string packageName = args[0];
            // Check if the package exists
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/Novixx-Systems/eaucool-packages/main/" + packageName + ".eau");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Package not found");
                return;
            }
            // Get the package
            string package = await response.Content.ReadAsStringAsync();
            // Create the directory
            if (!Directory.Exists("eaumods"))
            {
                Directory.CreateDirectory("eaumods");
            }
            // Write the package
            File.WriteAllText(Path.Combine("eaumods", packageName + ".eau"), package);
            // Done
            Console.WriteLine("Installed " + packageName);
            Console.WriteLine("Run Eaucool to use it");
        }
    }
}