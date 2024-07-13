Simple string:string database that you can use as a C# dictionary. 
It's not a great choice for large amounts of data, but good enough for storing simple variables such as players' total time spent on your server.
Add the `.dll` to your dependencies, and create a database using `TextDatabase.Open("your_database_name")`. It will contain everything from previous times you've used it, and automatically sync with the text file, allowing multiple servers to use it at the same time.
Be careful not to edit the character you chose as the key separator, as doing this will cause an exception.
