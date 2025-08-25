# TCS ML-Agents Migration Tools

This directory contains utilities and tools to help migrate from legacy inheritance-based ML-Agents systems to the new composition-based system.

## Overview

The migration tools provide:

1. **Migration Utilities** - Automated tools to convert legacy agents
2. **Component Setup Tools** - Editor tools for quickly setting up components
3. **Validation Tools** - Tools to validate migrated scenarios
4. **Configuration Validators** - Tools to validate configuration files

## Migration Utilities

Located in `Editor/Migration/MigrationUtilities.cs`, these utilities help automate the migration process:

### Key Features:
- **Automated Component Migration** - Converts legacy Agent components to composition system
- **Observation System Migration** - Migrates observation collection to VectorObservationCollector
- **Action System Migration** - Migrates action handling to ActionDistributor
- **Reward System Migration** - Migrates reward calculation to RewardCalculator
- **Episode Management Migration** - Migrates episode handling to EpisodeManager
- **Sensor System Migration** - Migrates sensors to SensorManager
- **Configuration Generation** - Creates MLBehaviorConfig assets

### Usage:
```csharp
// Migrate a legacy agent
bool success = MigrationUtilities.MigrateLegacyAgent(legacyAgent, targetGameObject);

// Validate the migration
var validationResult = MigrationUtilities.ValidateMigration(targetGameObject);

// Generate a migration report
string report = MigrationUtilities.GenerateMigrationReport(targetGameObject);
```

## Component Setup Tool

Located in `Editor/ComponentSetupTool.cs`, this editor window provides a GUI for quickly setting up ML-Agents components:

### Features:
- **Quick Setup Presets** - Basic, Complete, and Sensor-Rich setups
- **Component Selection** - Choose which components to add
- **Validation** - Check current setup completeness
- **One-Click Setup** - Add multiple components with a single click

### Usage:
1. Open via `TCS ML-Agents > Component Setup Tool`
2. Select target GameObject
3. Choose components to add
4. Click "Setup Selected Components"

## Migration Validation Tool

Located in `Editor/MigrationValidationTool.cs`, this tool validates migrated scenarios:

### Features:
- **Component Validation** - Check for missing core components
- **Configuration Validation** - Validate configuration assets
- **Performance Analysis** - Check for potential performance issues
- **Best Practice Validation** - Check for common mistakes
- **Detailed Analysis** - Deep analysis of component interactions
- **Report Generation** - Export validation reports

### Usage:
1. Open via `TCS ML-Agents > Migration Validation Tool`
2. Select migrated GameObject
3. Run validation or detailed analysis
4. Review results and fix issues
5. Export reports if needed

## Configuration Validator

Located in `Runtime/Core/Validation/ConfigurationValidator.cs`, this utility validates MLBehaviorConfig assets:

### Features:
- **Property Validation** - Check configuration properties
- **Space Validation** - Validate observation and action spaces
- **Training Validation** - Validate training parameters
- **Comparison** - Compare configurations for differences
- **Warning System** - Flag potential issues

### Usage:
```csharp
// Validate a configuration
var result = ConfigurationValidator.ValidateConfiguration(config);

if (result.IsValid) {
    // Configuration is valid
    Debug.Log("Configuration is valid!");
} else {
    // Configuration has errors
    foreach (var error in result.Errors) {
        Debug.LogError($"Configuration error: {error}");
    }
}

// Compare two configurations
var comparison = ConfigurationValidator.CompareConfigurations(config1, config2);
if (!comparison.AreIdentical) {
    foreach (var difference in comparison.Differences) {
        Debug.LogWarning($"Configuration difference: {difference}");
    }
}
```

## Configuration Validation Window

Located in `Editor/ConfigurationValidationWindow.cs`, this editor window provides a GUI for validating configurations:

### Features:
- **Quick Validation** - Validate specific aspects of configurations
- **Error Reporting** - Clear error and warning messages
- **Asset Creation** - Create new configuration assets

### Usage:
1. Open via `TCS ML-Agents > Configuration Validation`
2. Select configuration asset
3. Run validation or specific checks
4. Review results and fix issues

## Migration Process

### 1. Preparation
- Backup your project
- Identify legacy agents to migrate
- Document current behavior

### 2. Component Migration
- Use MigrationUtilities to convert agents
- Review and adjust migrated components
- Add missing components as needed

### 3. Configuration
- Create MLBehaviorConfig assets
- Configure observation/action/reward spaces
- Set training parameters

### 4. Validation
- Use MigrationValidationTool to check setup
- Fix any reported issues
- Test in play mode

### 5. Testing
- Verify behavior matches original
- Check performance characteristics
- Optimize as needed

## Best Practices

### During Migration:
- Migrate one agent at a time
- Keep backups of original agents
- Test frequently during migration
- Document changes made

### After Migration:
- Validate all components are properly configured
- Test edge cases and error conditions
- Profile performance and optimize if needed
- Update documentation

### Common Issues:
- Missing observation providers
- Incorrect action space configuration
- Reward scaling issues
- Performance bottlenecks
- Configuration mismatches

## Troubleshooting

### Missing Components:
- Check MigrationValidationTool reports
- Add required components manually
- Verify component configuration

### Performance Issues:
- Reduce observation space size
- Optimize raycast sensors
- Minimize reward provider count
- Profile and identify bottlenecks

### Configuration Problems:
- Use ConfigurationValidator to identify issues
- Compare with working configurations
- Check documentation for proper values

## Support

For issues with migration tools, contact the development team or check the documentation.

Happy migrating!