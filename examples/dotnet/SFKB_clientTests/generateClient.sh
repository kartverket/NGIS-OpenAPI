nvm install node
npm install -g nswag yamljs
yaml2json -s ../../../spec/openapi.yaml
nswag swagger2csclient /input:../../../spec/openapi.json /classname:Client /namespace:SFKB_API /output:Client.cs