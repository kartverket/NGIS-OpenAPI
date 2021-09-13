npm install -g nswag@13.7.0 yamljs
yaml2json -s ../../../spec/openapi.yaml
nswag swagger2csclient /input:../../../spec/openapi.json /classname:Client /namespace:SFKB_API /output:Client.cs