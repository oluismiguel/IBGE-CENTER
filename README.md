# IBGE Center – Nomes mais comuns por localidade (.NET MAUI)

Aplicativo Android que consome **duas APIs públicas do IBGE em cadeia**: o usuário escolhe um estado ou município em um dropdown e o app consulta automaticamente a API de Nomes (Censo 2022), exibindo os nomes masculinos, femininos e os sobrenomes mais comuns da localidade escolhida.

## 👥 Integrantes

- Luis Miguel Vitor Miranda da Silva
- João Pedro
- Lucca

> ⚠️ Antes de entregar, substitua "João Pedro" e "Lucca" pelos **nomes completos** (sobrenomes incluídos), conforme exigido pela atividade.

## 🔗 APIs utilizadas

| Etapa | Endpoint |
|---|---|
| 1. Localidades (estados) | `https://servicodados.ibge.gov.br/api/v1/localidades/estados` |
| 1. Localidades (municípios) | `https://servicodados.ibge.gov.br/api/v1/localidades/municipios` |
| 2. Nomes por localidade | `https://servicodados.ibge.gov.br/api/v3/nomes/2022/localidade/{id_localidade}` |

Fluxo encadeado: o `id` da localidade selecionada no dropdown é usado diretamente na URL da API de Nomes, sem botão extra.

> Caso o endpoint v3 não responda ou venha vazio, o app usa como contingência o ranking v2 do IBGE (`/api/v2/censos/nomes/ranking?localidade={id}&sexo=M|F`). Nesse caso os sobrenomes não são exibidos.

## 🏗️ Arquitetura em camadas

```
IbgeCenter/
├── Models/            CAMADA DE MODELOS – classes que mapeiam o JSON
│   ├── Estado.cs, Municipio.cs        (API de Localidades)
│   ├── NomeFrequencia.cs, NomesResultado.cs   (API de Nomes)
│   └── Localidade.cs                  (item do dropdown)
├── Services/          CAMADA DE SERVIÇOS / INTEGRAÇÃO – HttpClient + JSON
│   ├── LocalidadesService.cs
│   └── NomesService.cs
├── ViewModels/        Estado da tela e reações às ações do usuário (MVVM)
│   └── MainViewModel.cs
├── Views/             CAMADA DE APRESENTAÇÃO – XAML (Picker, listas)
│   └── MainPage.xaml
├── Helpers/           Utilitários de texto (busca sem acento, capitalização)
├── Platforms/Android/ Arquivos de plataforma (permissão INTERNET)
└── MauiProgram.cs     Injeção de dependência
```

Fluxo: `View → ViewModel → Service → API do IBGE`. A interface não faz nenhuma requisição HTTP diretamente.

## ✨ Funcionalidades
- Dropdown de **Estado** ou **Município** (5.570 municípios, com campo de filtro sem diferenciar acentos).
- Consulta automática dos nomes ao selecionar a localidade.
- Listas separadas de nomes masculinos, femininos e sobrenomes, com posição no ranking e frequência.
- Indicador de carregamento e mensagens de erro (sem internet, API fora do ar etc.).
- Cache em memória das listas de localidades.

## ▶️ Como executar

**Pré-requisitos**
- .NET 10 SDK
- Workload MAUI: `dotnet workload install maui`
- Android SDK + um emulador Android **com acesso à internet**

**Pelo terminal**
```bash
cd IbgeCenter
dotnet restore
dotnet build -t:Run -f net10.0-android
```
(com o emulador já iniciado)

**Pelo Visual Studio 2022/2026:** abra o `IbgeCenter.csproj`, escolha o emulador Android e pressione **F5**.

## 🧪 Roteiro de demonstração (vídeo de até 3 min)
1. Abrir o app e mostrar o dropdown de **Estados** carregado da API.
2. Selecionar um estado (ex.: Minas Gerais) e mostrar as listas de nomes masculinos, femininos e sobrenomes.
3. Trocar para **Município**, digitar "Varginha" no filtro, selecionar e mostrar o resultado.
4. Comparar com outro estado para evidenciar a diferença regional.

## 📦 Dependências (NuGet)
`CommunityToolkit.Mvvm`, `Microsoft.Extensions.Logging.Debug`

## 📤 Entrega
- Repositório público no GitHub com este código e README.
- Link do vídeo (YouTube/Google Drive) com a demonstração no emulador Android.
