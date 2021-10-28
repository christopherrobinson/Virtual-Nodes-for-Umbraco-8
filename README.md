# Virtual Nodes for Umbraco 9

Basically a rewrite of [Umbraco-VirtualNodes](https://github.com/sotirisf/Umbraco-VirtualNodes/) from [Sotiris Filippidis](https://github.com/sotirisf/) to make it compatible with Umbraco 9.0+.

This plugin lets you define document types that will be excluded from generated URLs., thus making them "invisible".

## Usage
After you include this plugin you must have to add a single node to your `appSettings.json` file, e.g.

```json
"VirtualNodes" {
	"Rules": "docTypeName"
}
```
Where docTypeName is the document type alias to be treated as a "virtual" node.

You can define multiple "rules" by separating them with commas, e.g.

```json
"VirtualNodes" {
	"Rules": "docTypeName,anotherDocType"
}
```

You can also use wildcards at the start and/or the end of the document type alias, like this:

```json
"VirtualNodes" {
	"Rules": "dog*,*cat,*mouse*"
}
```

This means that all document type aliases ending with "dog", starting with "cat" or containing "mouse" will be treated as virtual nodes. 

## Advanced: Auto numbering of nodes

Consider the following example:

```
articles
  groupingNode1
    article1
    article2
  groupingNode2
```   
 
Supposing that groupingNode1 and groupingNode2 are virtual nodes, the path for article1 will be /articles/article1. Okay, but what if we add a new article named "article1" under groupingNode2?

The plugin checks nodes on save and changes their names accordingly to protect you from this. More specifically, it checks if the parent node of the node being saved is a virtual node and, if so, it checks all its sibling nodes to see if there is already another node using the same name. It then adjusts numbering accordingly.

So, if you saved a new node named "article1" under "groupingNode2" it would become:

```
articles
  groupingNode1
    article1
    article2
  groupingNode2
    article1 (1)
```

And then if you saved another node named "article1" again under "groupingNode1" it would become "article1 (2)" like this:

```
articles
  groupingNode1
    article1
    article2
    article1 (2)
  groupingNode2
    article1 (1)
```

## Known issues

To keep things simple the auto numbering of nodes only go one level up - if you have multiple virtual nodes under each other and multiple nodes with the same name in different levels then you will run into problems.
