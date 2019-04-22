module Components.Feed

open System.Text.RegularExpressions
open Fable.Import
open StaticWebGenerator
open Util.Literals

module Node = Fable.Import.Node.Exports

let private feedFormat = sprintf """
<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom" xmlns:dc="http://purl.org/dc/elements/1.1/">
  <channel>
    <title>Fable Blog</title>
    <description></description>
    <link>https://fable.io/blog</link>
    <atom:link href="https://fable.io/blog/feed.xml" rel="self" type="application/rss+xml"/>
    <pubDate>%s</pubDate>
    <lastBuildDate>%s</lastBuildDate>%s
  </channel>
</rss>"""

let private itemFormat = sprintf """
      <item>
        <dc:creator>%s</dc:creator>
        <title>%s</title>
        <description>%s</description>
        <pubDate>%s</pubDate>
        <link>https://fable.io/blog/%s</link>
        <guid isPermaLink="true">https://fable.io/blog/%s</guid>
      </item>"""

let renderFeed() =
  let path = Node.path.join(Paths.DeployDir, "blog", "feed.xml")
  let text = Node.fs.readFileSync((Node.path.join(Paths.BlogDir, "index.md"))).toString()
  let headerReg = Regex(@"\[(.*)\]\((.*)\)")
  let lineReg = Regex(@"\[(.*)\].* on (.*)")

  let posts = seq {
    let mutable header = ("", "")
    let mutable author = ("", "")
    for line in text.Split([| '\n' |], System.StringSplitOptions.RemoveEmptyEntries) do
      if line.StartsWith("##") then
        let title = headerReg.Match(line).Groups.[1].Value
        let link = headerReg.Match(line).Groups.[2].Value
        header <- (title, link)
      if line.StartsWith(">") then
        let m = lineReg.Match(line)
        let name = m.Groups.[1].Value
        let date = m.Groups.[2].Value
        author <- (name, date)

      if fst author <> "" then
        yield 
          {| Header = fst header; 
             Link = snd header
             Author = fst author; 
             Date = snd author; |} //TODO wrong format
        author <- ("", "")
        header <- ("", "")
  }

  //TODO Wrong Format!
  let now = System.DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss K")

  let items =
    posts
    |> Seq.map (fun post -> itemFormat post.Author post.Header "" post.Date post.Link post.Link)

  feedFormat now now (items |> String.concat "")
  |> IO.writeFile path

  printfn "Feed generated"