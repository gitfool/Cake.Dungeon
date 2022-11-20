#load bootstrap.cake

public string FileReadText(FilePath file) => System.IO.File.ReadAllText(file.MakeAbsolute(Context.Environment).FullPath);

public FilePathCollection GetFiles(IEnumerable<string> patterns) =>
    new FilePathCollection(patterns.Select(pattern => Context.Globber.GetFiles(pattern)).SelectMany(paths => paths));
