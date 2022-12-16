namespace EmulatorRC.API.Model.Bridge.Config;

public class BridgeOptions
{
    public const string Bridge = "Bridge";


    public string Name { get; set; }

    public BridgeListener Outer { get; set; }

    public BridgeListener Inner { get; set; }
    
}


public class BridgeListener
{
    public int TcpPort { get; set; }

    public int Buffer { get; set; }
    public string FakeImagePath { get; set; }

}
