using MessagesAPI.Manager;
using System;

namespace MessagesAPI.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            OrderManager messagesManager = new OrderManager();

            if (args.Length > 0 && args[0] == "r")
            {
                Console.WriteLine("RePost");
                messagesManager.RePostToBiz();
            }
            else
                messagesManager.ProcessMessages();
        }
    }
}
