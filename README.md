# MusicParser
[![Build](https://github.com/lorenzonicolas/musicParser2/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/lorenzonicolas/musicParser2/actions/workflows/build.yml)
## Como ejecutar
*Sin ningun parametro*: corre el lifecycle.

*service*: Corre como servicio, reaccionando cada vez que se crea un directorio en `folderToProcess`. Corre el `lifecycle` para cada carpeta nueva.

*Lifecycle*: fixea los discos nuevos. Se basa en la config `folderToProcess`. Lee todo lo que está ahí y lo intenta fixear por completo. Si lo logra, lo mueve a la carpeta `done_dir`. Sino, irá a alguna carpeta de error. Puede que vaya a casi listo, si solo hace falta confirmacion de tags (asegurar el género o país, por ejemplo).

*Tagfix*: este va a la carpeta definida en `tag_fix_dir`. Se fija qué falta fixear de tags. Es con interacción, hay que ir uno por uno a ver qué falló.

*CountryFix*: Sirve para fixear la metadata. Hay muchas bandas sin país, de cuando no tenía andando la metal-archives api. Correr para fixearlos, es automático no pide entry manual por el momento.

*Resync*: vuelve a armar el archivo de backup y metadata. ATENCION. Configurar bien `folderToProcess` antes de correr este, ya que puede pisar todo el archivo backup.

## Configuración
 - Configurar Nuget de GitHub manualmente: 
    `dotnet nuget add source "https://nuget.pkg.github.com/lorenzonicolas/index.json" --name "github_local" --username "REEMPLAZAR_USERNAME" --password "REEMPLAZAR_PASSWORD" --store-password-in-clear-text`
 - Configurar un repositorio local de nuget: `dotnet nuget add source D:\Repositorios\packages --name "Local"`

### Como tener todo corriendo en docker:
##### Create the network:
`docker network create mongo-network`

##### Create/run the mongo image
`docker run -d -p 27017:27017 --network mongo-network --name mongo mongo:4`

##### Build and run the metal archives api
`docker build -t metal_archives_api .`
`docker run -d -p 3000:3000 --network mongo-network --name metalarchives metal_archives_api`

As part of the build, it will try to execute `catchDB.js` but the connection string will point to the docker container - it will fail.
So let that step fail, it will build anyways, but then enter in the container and run catchDB manually (`npm run catchDB`).
https://www.howtogeek.com/devops/how-to-run-mongodb-in-a-docker-container/

### Como levantar la MetalArchives API localmente
- Docker corriendo
- Tener la imagen del Mongo DB corriendo y expuesto en :27017
- Instalar dependencias: `npm install`
- (Opcional) re-cachear la base de datos: `npm run catchDB `
- Correr la api: `npm start`

### Testing y coverage report:
`dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`
`reportgenerator -reports:"MusicParser.Tests.\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html`

### Ideas de mejoras
[ ] E2E tests
[X] Unit tests
[X] Que la MetalArchives API corra en docker en vez de local
[X] Que la MongoDB corra en docker en vez de local
[ ] Healthcheck para de una detectar que las apis estén levantadas (Spotify y MetalArchives)
[ ] Que con un solo comando levante ambas imagenes y este todo corriendo (docker compose?)
[ ] Sonar-Cube gratuito para análisis de code smells y errores
[ ] Comentar en el PR el resultado del análisis de SonarCube
[X] Pipeline que buildee / corra unit tests en cada PR o commit / push
[X] Pipeline con un threshold de % de cobertura de UT - que falle el PR si no se cumple
[ ] Mejorar como se buildea la aplicacion en base a los argumentos
[X] Que use paquetes remotos míos

## Errores
- Que no updatee el backupfile en cada nuevo disco detectado y que lo haga una sola vez
- Que no descargue el archivo de google en el startup.