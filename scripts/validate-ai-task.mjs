import fs from "node:fs";
import Ajv from "ajv";
import addFormats from "ajv-formats";

const schemaPath = ".ai/schemas/no-discovery-agent-task.schema.json";
const inputPath = process.env.AI_TASK_FILE || ".ai/task.json";

const schema = JSON.parse(fs.readFileSync(schemaPath, "utf8"));
const input = JSON.parse(fs.readFileSync(inputPath, "utf8"));

const ajv = new Ajv({ allErrors: true, strict: false });
addFormats(ajv);

const validate = ajv.compile(schema);
const ok = validate(input);

if (!ok) {
  console.error("❌ AI task schema validation failed:");
  for (const err of validate.errors ?? []) {
    console.error(`- ${err.instancePath || "/"} ${err.message}`);
  }
  process.exit(1);
}

console.log("✅ AI task schema validation passed");
