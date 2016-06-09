require 'rexml/document'

task :create_test_connection_config, :server, :namespace, :plugin_name do |t, args|
	template = "Server=SERVER;Database=NAMESPACE_TestDBNAME;Integrated security=true"
	test_dir = args.plugin_name + "Tests"
	settings = REXML::Document.new File.new(test_dir + "/Tests.Common.dll.config.template")
	
	elements = {
		"DataExportManagerConnectionString" => "DataExportManager",
		"DataQualityEngineConnectionString" => "DataQualityEngine",
		"TestCatalogueConnectionString" => "Catalogue",
		"UnitTestLoggingConnectionString" => "Logging"
	}

	elements.each do |element_name, database_name|
		connection = settings.root.elements[element_name]
		connection.text = HelperFunctions.substitute(template, args.server, args.namespace, database_name);
	end

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