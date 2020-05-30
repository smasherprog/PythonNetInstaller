# PythonNetInstaller

<h3>Getting Started</h3>
<p>Install the <a href="https://www.nuget.org/packages/PythonNetInstaller">nuget package</a></p>
<p>Things to be aware of: </br>
  The installation is local and will only be available to the current process, meaninig this will not affect any current installations. Pip is also supported as well but you will need to call the function to install it after you have installed python</br>
You have three different ways to install python:
<ul>
  <li>From Url</li>
  <li>From File</li>
  <li>From Embeded resource</li>
</ul>
Below are some examples of how you can get started
</p>


PythonNetInstaller.Installer.DownloadAndInstallPython("https://www.python.org/ftp/python/3.8.3/python-3.8.3-embed-amd64.zip");
            
PythonNetInstaller.Installer.InstallPythonFromAssembly(Assembly.GetExecutingAssembly(), "python-3.7.3-embed-amd64.zip");
           
var p = Path.Combine(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, System.AppDomain.CurrentDomain.RelativeSearchPath ?? ""), "python-3.7.7-embed-amd64.zip");
PythonNetInstaller.Installer.InstallPythonFromZip(p);

PythonNetInstaller.Installer.InstallPip();

<p>After this, you can run python commands like this:
  <ul>
  <li>PythonNetInstaller.Installer.RunCommand("python.exe args_go_here");</li>
  <li>PythonNetInstaller.Installer.RunCommand("pip install tensorflow");</li> 
</ul>
</p>
<p>This library is best suited for use with <a href="http://pythonnet.github.io/">http://pythonnet.github.io/</a></p>
<p>for example, after installing tensorflow, you can import it into your .NET application to load a model:</p>

if (!PythonEngine.IsInitialized)
  PythonEngine.Initialize();
var tensorflow = Py.Import("tensorflow");
var aimodel= tensorflow.keras.models.load_model("docai_encf2.h5");
aimodel.load_weights("docai_encf2_w.h5");
