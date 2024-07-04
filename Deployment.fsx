#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open System

/// Creates a PgSql and web app resources for a specific environment e.g. "dev" or "test".
let createEnvironment envName =
    let appName = "myApp"
    let dbServerName = $"%s{appName}-dbserver-{envName}"

    // Currently deploys a single-instance PostgreSQL server
    // Farmer is being updated to support Flexible databases.
    let pgServer =
        let database = postgreSQLDb { name appName }

        postgreSQL {
            name dbServerName
            add_database database
            capacity 1<VCores>
            admin_username "dbsuperuser"
        }

    let logStore = logAnalytics { name $"{appName}-logstore-{envName}" }

    let insights =
        appInsights {
            name $"{appName}-insights-{envName}"
            log_analytics_workspace logStore
        }

    let app =
        webApp {
            name $"{appName}-web-{envName}"

            setting "DbDomainName" pgServer.FullyQualifiedDomainName
            secret_setting "DbPassword"

            operating_system Linux
            runtime_stack Runtime.DotNet80
            run_from_package
            zip_deploy "src/App/bin/release/net8.0/publish"

            sku WebApp.Sku.B1
            always_on

            link_to_app_insights insights
        // uncomment next line once Farmer 1.8.13 is released.
        //depends_on pgServer
        }

    let rg =
        resourceGroup {
            location Location.WestEurope
            add_resources [ pgServer; app; insights; logStore ]
            add_tags [ "environment", envName ]
        }

    rg, dbServerName

let envName = "test"

let deployment, dbServerName = createEnvironment envName

deployment |> Writer.quickWrite "template"

let dbPassword =
    match Environment.GetEnvironmentVariable "DbPassword" with
    | null -> failwith "DbPassword environment variable not set"
    | value -> value

deployment
|> Deploy.execute $"impactDemo-{envName}" [ $"password-for-{dbServerName}", dbPassword; "DbPassword", dbPassword ]