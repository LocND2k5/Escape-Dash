$sprites = @(
    @{ Name = "board_background.png"; Guid = "12c27d97c823465c9be80edcbd98c078"; Border = "x: 64, y: 64, z: 64, w: 64" },
    @{ Name = "header_banner.png";    Guid = "4cbfccad8157458aa00696f8c81d2b09"; Border = "x: 64, y: 40, z: 64, w: 40" },
    @{ Name = "crown.png";            Guid = "683ec05d38b543dea70a86b362177e27"; Border = "x: 0, y: 0, z: 0, w: 0" },
    @{ Name = "close_button.png";     Guid = "e5f715d6c3394bf593f1105af31b114c"; Border = "x: 0, y: 0, z: 0, w: 0" },
    @{ Name = "rank_badge.png";       Guid = "ef25bc0d1dd1496882017218cf1b70a3"; Border = "x: 28, y: 28, z: 28, w: 28" },
    @{ Name = "score_badge.png";      Guid = "7cbc43a5e5d640d0a7c5e28e2fd8cb26"; Border = "x: 48, y: 32, z: 48, w: 32" },
    @{ Name = "row_divider.png";      Guid = "f45f45e8e53f4c6cba3246a47d3095f2"; Border = "x: 0, y: 0, z: 0, w: 0" }
)

$template = @"
fileFormatVersion: 2
guid: {GUID}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {{BORDER}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

foreach ($s in $sprites) {
    $metaContent = $template.Replace("{GUID}", $s.Guid).Replace("{BORDER}", $s.Border)
    $path = "Assets\Sprites\Leaderboard\$($s.Name).meta"
    [System.IO.File]::WriteAllText($path, $metaContent)
    Write-Host "Created $path"
}
