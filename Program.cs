using System;
using System.IO;
using System.Threading.Tasks;

using System.Text.Json;
using System.Text.Json.Nodes;

using System.Text;
using System.Text.Encodings.Web;

using System.Linq;

using System.Collections.Generic;

using Fluid;

using ProjectStructure;

var solution = new Solution("eu1_solution");

var site = new Project("eu2_site")
{
    Resources = {
        new SiteConfigResource
        {
            BaseDomain = "gcli.us1.prod.site.com",
            DataCenter = "us1",
            TrustedSiteUrls = new [] {"gcli.us1.prod.site.com/*", "*.gcli.us1.prod.site.com/*"},
            Description = """
            {{ for name in names }}
            {{name}}
            {{ end }}
            {{ bool.true }}
            { { bool.false } }
            """,
            Tags = new string[] { }
        },
		new SiteSchemaCollectionResource
        {
            Resources = {
                new SchemaResource
                {
                    Name = "profile",
                    SchemaUri = "https://gist.githubusercontent.com/EugeneKrapivin/a8adfe1bbac7b0bf80497a456ea91822/raw/fe37ae3e097317a27e5aea193d4ba9ef683420fe/schema.data.json",
                    Schema = """
                    {
                      "fields": {
                        "lastName": {
                          "required": false,
                          "type": "string",
                          "writeAccess": "serverOnly",
                          "encrypt": "AES"
                        },
                        "lastName": {
                          "required": false,
                          "type": "string",
                          "writeAccess": "serverOnly",
                          "encrypt": "AES"
                        }
                      }
                    }
                    """
				}
			}
		},
		new ScreenSetsResouce
        {
            Resources = {
                new ScreenSetResource("LoginRegistration")
                {
                    Html = """<html></html>""",
                    Javascript = "some javascript",
                    Css = "some css",
                    RawTranslations = "should probably be a json",
                    Translations = new()
                    {
                        ["en"] = new ()
                        {
                            ["hello"] = "hello",
                            ["world"] = "world"
                        },
                        ["he"] = new ()
                        {
                            ["hello"] = "שלום",
                            ["world"] = "עולם"
                        }
                    }
                },
                new ScreenSetResource("Preferences")
                {
                    Html = """<html></html>""",
                    Javascript = "some javascript",
                    Css = "some css",
                    RawTranslations = "should probably be a json",
                }
                ,
                new ScreenSetResource("Account")
                {
                    Html = """<html></html>""",
                    Javascript = "some javascript",
                    Css = "some css",
                    RawTranslations = "should probably be a json",
                }
            }
        }
	}
};

solution.AddResource(site);

var model = JsonNode.Parse("""
{	
    "names" : ["emma", "liya", "liam"], 
    "bool": {
        "true": true,
        "false": false
    },
    "nested": {
        "bool": {
            "true": true,
            "false": false
        }
    },
    "nested_array": [{
        "true": true,
        "false": false
    }, {
        "true": true,
        "false": false
    },{
        "true": true,
        "false": false
    }]
}
""");


var templateVisitor = new TemplatingVisitor(model.AsObject());

await solution.Accept(templateVisitor);

await solution.PersistToDisk(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ProjectSolution"));