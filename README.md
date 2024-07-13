Simple string:string database that you can use as a C# dictionary. 
Not a great choice for large amounts of data, but good enough for storing simple variables such as players' total time spent on your server.

## How to use
1. Add `TextDb.dll` from [releases page](https://github.com/Banalny-Banan/TextDatabase/releases) to your dependencies.
2. Create a new database using `TextDatabase.Open("your_database_name")`. You can interact with it as you would with a normal dictionary.

Also dont try to save `ꨘ`, it is used as key/value separator.
