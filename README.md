Simple string:string database that you can use as a C# dictionary. 
Not a great choice for large amounts of data, but good enough for storing simple variables such as players' total time spent on your server.

## How to use
1. Add `TextDB.dll` from [releases page](https://github.com/Banalny-Banan/TextDatabase/releases) to your dependencies.
2. Create a new database using `TextDatabase.Open("your_database_name")`. You can interact with it as you would with a normal dictionary.

Also don't try to save `Ǽ`, it is used as key/value separator.

## Q&A:
 **Q:** Can I use the same database on multiple servers?

- **A:** Yes, but make sure to not make any edits that cause conflicts between servers, as they **wont** throw exceptions when made.

**Q:** How to store numbers/dates/etc.?

* **A:** Convert them to strings! All default C# types have `.ToString()` and `.Parse()` that can turn them into and from strings.
