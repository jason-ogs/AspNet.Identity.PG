This is library allows developers to use PostgreSQL as a backend store for the ASP.Net (not core) Identity middleware. While it is meant to be a full port of Microsoft.AspNet.Identity.EntityFramework, there are a couple of caveats:
 1. All DB keys are implemented as auto-incrementing integers.  I may come back and add string support at a future date, but it is not a high priority.
 2. There is some (very) light support for multi-tenant scenarios.
 3. It has only been minimally tested.
 4. The scripts to create the PG tables are in the test project, in the PostgresHelper.cs file.
 5. Postgres column data is case sensitive in regards to queries.  There are several ways to account for this.  I have chosen
to use the lower() function and add an index to help performance.  This may or may not work for your needs.

Please let me know if you find any bugs.  Thanks!

The project is being released under the terms of the MIT license:

Copyright (c) 2016 Jason Bennett

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


