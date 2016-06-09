require 'rexml/document'

task :create_test_connection_config, :server, :namespace, :plugin_name do |t, args|
	template = "Server=SERVER;Database=NAMESPACE_TestDBNAME;Integrated security=true"
	test_dir = args.plugin_name + "Tests"
	settings = REXML::Document.new File.new(test_dir + "/Tests.Common.dll.config.template")
	
	connection = settings.root.elements["DataExportManagerConnectionString"]
	connection.text = HelperFunctions.substitute(template, args.server, args.namespace, "DataExportManager");

	connection = settings.root.elements["DataQualityEngineConnectionString"]
	connection.text = HelperFunctions.substitute(template, args.server, args.namespace, "DataQualityEngine");

	connection = settings.root.elements["TestCatalogueConnectionString"]
	connection.text = HelperFunctions.substitute(template, args.server, args.namespace, "Catalogue");

	connection = settings.root.elements["UnitTestLoggingConnectionString"]
	connection.text = HelperFunctions.substitute(template, args.server, args.namespace, "Logging");

	File.write(test_dir + "/Tests.Common.dll.config", settings.to_s)
end

class HelperFunctions
	def self.substitute(template, server, namespace, database_name)
		connection_string = template.gsub("SERVER", server)
		connection_string = connection_string.gsub("NAMESPACE", namespace)
		connection_string = connection_string.gsub("DBNAME", database_name)
		return connection_string
	end
end