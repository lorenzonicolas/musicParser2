# MusicParser

## Como ejecutar
 Sin ningun parametro: corre el lifecycle base.

 Lifecycle: fixea los discos nuevos. Se basa en la config `folderToProcess`. Lee todo lo que está ahí y lo intenta fixear por completo. Si lo logra, lo mueve a la carpeta `done_dir`. Sino, irá a alguna carpeta de error. Puede que vaya a casi listo, si solo hace falta confirmacion de tags (asegurar el género o país, por ejemplo).

 Tagfix: este va a la carpeta definida en `tag_fix_dir`. Se fija qué falta fixear de tags. Es con interacción, hay que ir uno por uno a ver qué falló.

 CountryFix: Sirve para fixear la metadata. Hay muchas bandas sin país, de cuando no tenía andando la metal-archives api. Correr para fixearlos, es automático no pide entry manual por el momento.

 Resync: vuelve a armar el archivo de backup y metadata. ATENCION. Configurar bien `folderToProcess` antes de correr este, ya que puede pisar todo el archivo backup.

 ### Como levantar la MetalArchives API
- Docker corriendo
- Tener la imagen del Mongo DB corriendo y expuesto en :27017
- Instalar dependencias: `npm install`
- (Opcional) re-cachear la base de datos: `npm run catchDB `
- Correr la api: `npm start`

## Ideas de mejoras
[] E2E tests
[] Unit tests
[] Que la MetalArchives API corra en docker en vez de local
[X] Que la MongoDB corra en docker en vez de local
[] Que con un solo comando levante ambas imagenes y este todo corriendo (docker compose)
[] Sonar-Cube gratuito para análisis de code smells y errores
[] Comentar en el PR el resultado del análisis de SonarCube
[X] Pipeline que buildee / corra unit tests en cada PR o commit / push
[X] Pipeline con un threshold de % de cobertura de UT - que falle el PR si no se cumple

## Errores
- que no updatee el backupfile en cada nuevo disco detectado y que lo haga una sola vez

- Que no descargue el archivo de google en el startup.