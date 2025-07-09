using OrderAccumulator.ConsoleApp;

class Program
{
    static void Main()
    {
        var bootstrapper = CompositionRoot.ConfigureFixServer();
        bootstrapper.Start();

        Console.WriteLine("Aguardando Orders... Pressione ENTER para sair.");
        Console.ReadLine();

        bootstrapper.Stop();
    }
}
