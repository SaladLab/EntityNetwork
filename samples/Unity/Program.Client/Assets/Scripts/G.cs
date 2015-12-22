using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;

public static class G
{
    static G()
    {
        _logger = LogManager.GetLogger("G");
    }

    // Communicator

    private static Communicator _comm;

    public static Communicator Comm
    {
        get { return _comm; }
        set { _comm = value; }
    }

    // Logger

    private static readonly ILog _logger;

    public static ILog Logger
    {
        get { return _logger; }
    }
}
