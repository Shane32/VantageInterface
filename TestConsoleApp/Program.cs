using VantageInterface;

//await Task.Delay(1);
var vConnection = await VConnection.ConnectAsync("172.18.0.9", default);
var vControl = new VControl(vConnection);

vControl.OnLoadUpdate += (object? source, VLoadEventArgs args2) =>
{
    Console.WriteLine($"Load #{args2.Vid} set to {args2.Percent}%");
};
vControl.OnTaskUpdate += (object? source, VTaskEventArgs args2) =>
{
    Console.WriteLine($"Task #{args2.Vid} new state {args2.State}");
};
vControl.OnButtonUpdate += (object? source, VButtonEventArgs args2) =>
{
    Console.WriteLine($"Button #{args2.Vid} {(args2.Mode == ButtonModes.Press ? "Pressed" : "Released")}");
};
vControl.OnLedUpdate += (object? source, VLedEventArgs args2) =>
{
    Console.WriteLine($"LED #{args2.Vid} set to state {args2.State}");
};

//await vControl.ConnectAsync();
//vControl.Connect();
Console.WriteLine("Getting load 219");
//vControl.UpdateLoad(219);
Console.WriteLine($"Value for load 219: {await vControl.Get.LoadAsync(219)}");
Console.ReadLine();
Console.WriteLine("Getting led 1591");
Console.WriteLine(await vControl.Get.LedAsync(1591));

Console.WriteLine("Setting to 0%");
vControl.Set.Load(219, 0);

Console.ReadLine();

Console.WriteLine("Setting to 50%");
vControl.Set.Load(219, 50);

Console.ReadLine();

Console.WriteLine("Setting to 200%");
vControl.Set.Load(219, 200);

Console.ReadLine();

vControl.Dispose();
vConnection.Dispose();

