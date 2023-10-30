using System.ServiceModel;

namespace MessagesAPI.Service
{
    [ServiceContract(Namespace = "http://tempuri.org/")]
    public interface IMessagesService
    {
        [OperationContract]
        string ToM2North(string envelope);
    }
}
